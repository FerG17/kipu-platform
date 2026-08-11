using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bodega.Platform.Tests.Infrastructure;

namespace Bodega.Platform.Tests;

/// <summary>
///     How alerts start, stop, and stay scoped to their own business. Each
///     test maps to a defect found in the 2026-08-10 independent audit.
/// </summary>
[Collection(BodegaApiCollection.Name)]
public class AlertsLifecycleTests(BodegaApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>
    ///     POST /alerts was the one command service with no ownership check,
    ///     so an admin could raise an alert naming another business's product.
    ///     Because the upsert lookup ignores query filters, the other business's
    ///     handler would then treat that row as its own existing alert and
    ///     refresh it instead of raising theirs — quietly suppressing it.
    /// </summary>
    [Fact]
    public async Task CreateAlert_ForAProductOfAnotherBusiness_IsRejected()
    {
        var victim = await CreateBusinessAsync();
        var victimProductId = await CreateProductAsync(victim);

        var attacker = await CreateBusinessAsync();
        var response = await attacker.PostAsJsonAsync("/api/v1/alerts", new
        {
            productId = victimProductId,
            batchId = (int?)null,
            productName = "robado",
            type = "LOW_STOCK",
            severity = "LOW",
            message = "alerta cruzada",
            currentStock = 0,
            minStock = 1
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    ///     Discarding a batch is what finally lets an expired one stop
    ///     alerting — it stayed ACTIVE forever, so the sweep re-raised the
    ///     same alert hours after each time it was resolved. Discarding also
    ///     closes what it left open, so it is one action rather than two.
    /// </summary>
    [Fact]
    public async Task DiscardingABatch_ClosesTheExpirationAlertItRaised()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        // Expiring in 2 days trips the default 7-day warning threshold.
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10,
            expiration: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2))).EnsureSuccessStatusCode();

        var batchId = await GetFirstBatchIdAsync(client, productId);
        Assert.Contains(await GetActiveAlertsAsync(client), alert => alert.GetProperty("type").GetString() == "EXPIRATION");

        (await client.PostAsync($"/api/v1/batches/{batchId}/discard", null)).EnsureSuccessStatusCode();

        Assert.DoesNotContain(await GetActiveAlertsAsync(client),
            alert => alert.GetProperty("batchId").GetInt32() == batchId);
    }

    /// <summary>Discarding twice is not a silent no-op — the second attempt is a conflict.</summary>
    [Fact]
    public async Task DiscardingABatchTwice_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 4,
            expiration: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20))).EnsureSuccessStatusCode();

        var batchId = await GetFirstBatchIdAsync(client, productId);
        (await client.PostAsync($"/api/v1/batches/{batchId}/discard", null)).EnsureSuccessStatusCode();

        var second = await client.PostAsync($"/api/v1/batches/{batchId}/discard", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    /// <summary>
    ///     Deactivating a product must close its alerts, or a low-stock
    ///     warning outlives the product and sits in the list pointing at
    ///     something already removed from the catalog.
    /// </summary>
    [Fact]
    public async Task DeactivatingAProduct_ClosesTheAlertsItLeftOpen()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        // Quantity 0 registers it in the warehouse and trips OUT_OF_STOCK.
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 0)).EnsureSuccessStatusCode();
        Assert.Contains(await GetActiveAlertsAsync(client), alert => alert.GetProperty("productId").GetInt32() == productId);

        (await client.DeleteAsync($"/api/v1/products/{productId}")).EnsureSuccessStatusCode();

        Assert.DoesNotContain(await GetActiveAlertsAsync(client),
            alert => alert.GetProperty("productId").GetInt32() == productId);
    }

    /// <summary>
    ///     isExpiringSoon was computed against the hardcoded 7-day default
    ///     instead of the business's configured AlertRule, so a shop that
    ///     warns at 30 days saw false on a batch that already had a live
    ///     EXPIRATION alert against it.
    /// </summary>
    [Fact]
    public async Task BatchIsExpiringSoon_FollowsTheBusinessConfiguredThreshold()
    {
        var client = await CreateBusinessAsync();
        (await client.PostAsJsonAsync("/api/v1/alert-rules",
            new { alertType = "EXPIRATION", thresholdValue = 30, enabled = true })).EnsureSuccessStatusCode();

        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        // 20 days out: beyond the 7-day default, well inside the configured 30.
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5,
            expiration: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20))).EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/v1/batches?productId={productId}");
        response.EnsureSuccessStatusCode();

        Assert.True((await ReadJsonAsync(response))[0].GetProperty("isExpiringSoon").GetBoolean(),
            "a batch 20 days out must count as expiring soon when the business warns at 30 days");
    }

    private static async Task<int> GetFirstBatchIdAsync(HttpClient client, int productId)
    {
        var response = await client.GetAsync($"/api/v1/batches?productId={productId}");
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response))[0].GetProperty("id").GetInt32();
    }

    private static async Task<List<JsonElement>> GetActiveAlertsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/alerts");
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).EnumerateArray().ToList();
    }
}

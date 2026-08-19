using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     I25 — manual stock adjustments (shrinkage, breakage, theft, count
///     corrections), the one stock-out path that previously did not exist at
///     all: a shop that threw out spoiled goods had no way to reflect that in
///     inventory outside of ringing up a fake sale.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class StockAdjustmentTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task AdjustStock_WithANegativeDelta_ReducesStockAndRecordsTheMovement()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 20)).EnsureSuccessStatusCode();

        var response = await AdjustStockAsync(client, productId, warehouseId, delta: -5, reason: "Merma: se cayó y rompió");

        response.EnsureSuccessStatusCode();
        Assert.Equal(15, await GetTotalStockAsync(client, productId));

        var movements = (await ReadJsonAsync(await client.GetAsync("/api/v1/stock-movements"))).EnumerateArray().ToList();
        var adjustment = Assert.Single(movements, movement => movement.GetProperty("type").GetString() == "ADJUSTMENT");
        Assert.Equal(-5, adjustment.GetProperty("quantity").GetInt32());
        Assert.Equal("Merma: se cayó y rompió", adjustment.GetProperty("note").GetString());
    }

    [Fact]
    public async Task AdjustStock_WithAPositiveDelta_IncreasesStock()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var response = await AdjustStockAsync(client, productId, warehouseId, delta: 4, reason: "Corrección de conteo físico");

        response.EnsureSuccessStatusCode();
        Assert.Equal(14, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task AdjustStock_WithZeroDelta_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var response = await AdjustStockAsync(client, productId, warehouseId, delta: 0, reason: "algo");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdjustStock_WithoutAReason_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var response = await AdjustStockAsync(client, productId, warehouseId, delta: -2, reason: "  ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(10, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task AdjustStock_RemovingMoreThanAvailable_IsRejectedAndLeavesStockIntact()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 3)).EnsureSuccessStatusCode();

        var response = await AdjustStockAsync(client, productId, warehouseId, delta: -10, reason: "robo");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(3, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task AdjustStock_ForADeactivatedProduct_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();
        (await AdjustStockAsync(client, productId, warehouseId, delta: -5, reason: "vaciar antes de desactivar")).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/v1/products/{productId}")).EnsureSuccessStatusCode();

        var response = await AdjustStockAsync(client, productId, warehouseId, delta: 5, reason: "reactivar stock a mano");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Adjustments feed the same LOW_STOCK/OUT_OF_STOCK pipeline every other stock mutation does — an adjustment that empties a shelf must alert exactly like a sale would.</summary>
    [Fact]
    public async Task AdjustStock_ThatEmptiesAWarehouse_TriggersAnOutOfStockAlert()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        (await AdjustStockAsync(client, productId, warehouseId, delta: -5, reason: "todo se echó a perder"))
            .EnsureSuccessStatusCode();

        var alerts = (await ReadJsonAsync(await client.GetAsync("/api/v1/alerts"))).EnumerateArray();
        Assert.Contains(alerts, alert => alert.GetProperty("productId").GetInt32() == productId
            && alert.GetProperty("type").GetString() == "OUT_OF_STOCK");
    }

    private static async Task<HttpResponseMessage> AdjustStockAsync(HttpClient client, int productId, int warehouseId,
        int delta, string reason)
    {
        return await client.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment", new { warehouseId, delta, reason });
    }
}

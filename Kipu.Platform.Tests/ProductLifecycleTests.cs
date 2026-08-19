using System.Net;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     Product registration, stock intake and deletion. Each test here maps
///     to a concrete defect found in the 2026-08-10 independent audit.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class ProductLifecycleTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>
    ///     The core requirement is that every product carries an expiration
    ///     date and therefore produces expiration alerts. Registering stock is
    ///     the natural way to bring goods in, and the endpoint accepts an
    ///     expiration — so it has to end up on a real batch, not be dropped.
    /// </summary>
    [Fact]
    public async Task StockIntake_WithAnExpirationDate_RecordsABatchForTheProduct()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 20, expiration: expiration,
            purchasePrice: 4.5m)).EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/v1/batches?productId={productId}");
        response.EnsureSuccessStatusCode();
        var batches = await ReadJsonAsync(response);

        Assert.True(batches.GetArrayLength() > 0,
            "registering stock with an expiration date must create a batch, otherwise there is no traceability and no expiration alert can ever fire");
        Assert.Equal(expiration.ToString("yyyy-MM-dd"), batches[0].GetProperty("expiration").GetString());
    }

    /// <summary>
    ///     Batch.UpdateDetails used to overwrite Expiration unconditionally
    ///     while InventoryId already used `?? existing` to stay put when null
    ///     — an asymmetry found in the 2026-08-18 audit. In practice: a second
    ///     stock intake for the same product that only sets a purchase price
    ///     (the normal shape of receiving a purchase order, which doesn't
    ///     re-send the expiration) silently wiped the date the first intake
    ///     had already recorded.
    /// </summary>
    [Fact]
    public async Task StockIntake_WithOnlyAPurchasePrice_DoesNotClearAnAlreadyRecordedExpiration()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 20, expiration: expiration,
            purchasePrice: 4.5m)).EnsureSuccessStatusCode();

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10, purchasePrice: 5.0m))
            .EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/v1/batches?productId={productId}");
        response.EnsureSuccessStatusCode();
        var batches = await ReadJsonAsync(response);

        Assert.Equal(expiration.ToString("yyyy-MM-dd"), batches[0].GetProperty("expiration").GetString());
    }

    /// <summary>
    ///     MustBeAMoneyAmount used InclusiveBetween(0, max) — a product could
    ///     be registered with a sale price of exactly 0, and every sale of it
    ///     would then count as 0 revenue while still decrementing stock.
    /// </summary>
    [Fact]
    public async Task CreateProduct_WithZeroBasePrice_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await CreateProductResponseAsync(client, basePrice: 0m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///     Deleting a product is blocked while it still has stock, which means
    ///     the only deletable product is one at zero — exactly the one the
    ///     alert engine has flagged OUT_OF_STOCK. The foreign key from alerts
    ///     is RESTRICT, so the delete explodes into an unhandled database
    ///     error. Whatever the product policy ends up being, an internal
    ///     server error is never the right answer.
    /// </summary>
    [Fact]
    public async Task DeleteProduct_ThatHasAnAlert_DoesNotFailWithAnInternalServerError()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        // Quantity 0 registers the product in the warehouse and trips the
        // OUT_OF_STOCK alert, leaving it deletable as far as stock goes.
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 0)).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/v1/products/{productId}");

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    /// <summary>
    ///     RegisterStockIntake never checked the product's own Status — a
    ///     deactivated product could still receive stock (directly, or via a
    ///     received purchase order, which goes through the same handler),
    ///     defeating the point of deactivating it.
    /// </summary>
    [Fact]
    public async Task RegisterStockIntake_ForADeactivatedProduct_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await client.DeleteAsync($"/api/v1/products/{productId}")).EnsureSuccessStatusCode();

        var response = await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    ///     POST /products/{id}/activate undoes DeleteProduct's soft delete —
    ///     the product must disappear from the default (active-only) catalog
    ///     immediately after deletion, and come back after reactivation.
    /// </summary>
    [Fact]
    public async Task ActivateProduct_BringsADeactivatedProductBackIntoTheDefaultCatalog()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        (await client.DeleteAsync($"/api/v1/products/{productId}")).EnsureSuccessStatusCode();

        Assert.DoesNotContain((await ReadJsonAsync(await client.GetAsync("/api/v1/products"))).EnumerateArray(),
            product => product.GetProperty("id").GetInt32() == productId);

        (await client.PostAsync($"/api/v1/products/{productId}/activate", null)).EnsureSuccessStatusCode();

        Assert.Contains((await ReadJsonAsync(await client.GetAsync("/api/v1/products"))).EnumerateArray(),
            product => product.GetProperty("id").GetInt32() == productId);
    }
}

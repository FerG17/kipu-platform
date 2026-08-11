using System.Net;
using System.Net.Http.Json;
using Bodega.Platform.Tests.Infrastructure;

namespace Bodega.Platform.Tests;

/// <summary>
///     Money and stock invariants for the sales flow. Each test here maps to
///     a concrete defect found in the 2026-08-10 independent audit.
/// </summary>
[Collection(BodegaApiCollection.Name)]
public class SalesIntegrityTests(BodegaApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>
    ///     A POS that adds the same product twice produces two lines for one
    ///     product. Stock is validated per line against the *total* available,
    ///     so 2 lines × 3 units each both pass against 5 units in stock — but
    ///     together they ask for 6. The sale must not be accepted, and stock
    ///     must be left untouched.
    /// </summary>
    [Fact]
    public async Task Sale_AskingForMoreUnitsAcrossLinesThanAvailable_IsRejectedAndLeavesStockIntact()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        var response = await CreateSaleAsync(client,
            SaleLine(productId, quantity: 3, unitPrice: 10m),
            SaleLine(productId, quantity: 3, unitPrice: 10m));

        Assert.False(response.IsSuccessStatusCode,
            "a sale for 6 units of a product with only 5 in stock must be rejected");
        Assert.Equal(5, await GetTotalStockAsync(client, productId));
    }

    /// <summary>
    ///     Quantity is only ever checked as "is there enough stock", which a
    ///     negative number trivially satisfies. A negative line yields a
    ///     negative subtotal that lands in the revenue KPI.
    /// </summary>
    [Fact]
    public async Task Sale_WithNegativeQuantity_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var response = await CreateSaleAsync(client, SaleLine(productId, quantity: -100, unitPrice: 10m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///     Subtotal is quantity × unitPrice × (1 - discount), so any discount
    ///     above 1 flips the sale negative. The column is decimal(5,4), which
    ///     happily stores 2.0.
    /// </summary>
    [Fact]
    public async Task Sale_WithDiscountGreaterThanOneHundredPercent_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var response = await CreateSaleAsync(client, SaleLine(productId, quantity: 1, unitPrice: 10m, discount: 2m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///     Cancelling a sale means the goods came back, so the stock has to
    ///     come back with them. It used to just flip the status: the units
    ///     stayed deducted forever while the revenue disappeared, so the
    ///     inventory drifted below reality with every cancellation.
    /// </summary>
    [Fact]
    public async Task CancellingASale_ReturnsTheSoldUnitsToStock()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var saleResponse = await CreateSaleAsync(client, SaleLine(productId, quantity: 4, unitPrice: 10m));
        saleResponse.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(saleResponse)).GetProperty("id").GetInt32();
        Assert.Equal(6, await GetTotalStockAsync(client, productId));

        var cancelResponse = await client.PatchAsJsonAsync($"/api/v1/sales/{saleId}", new { status = "CANCELLED" });
        cancelResponse.EnsureSuccessStatusCode();

        Assert.Equal(10, await GetTotalStockAsync(client, productId));
    }

    /// <summary>
    ///     A sale that succeeds must always leave stock exactly reduced by
    ///     what it sold — the "todo o nada" guarantee the architecture claims.
    /// </summary>
    [Fact]
    public async Task Sale_ThatSucceeds_DecrementsStockByExactlyTheQuantitySold()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var response = await CreateSaleAsync(client, SaleLine(productId, quantity: 4, unitPrice: 10m));
        response.EnsureSuccessStatusCode();

        Assert.Equal(6, await GetTotalStockAsync(client, productId));
    }
}

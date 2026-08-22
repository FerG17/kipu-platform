using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X5 Bloque D: a product marked "se vende por peso" (Product.UnitOfSale
///     == PESO) may carry a fractional Quantity in a sale, a stock intake, or
///     a manual adjustment — a product left at the default UNIDAD may not,
///     since "2.5 latas de gaseosa" has no physical meaning. Quantity is
///     decimal(10,2) end to end now (SaleDetail, InventoryItem.StockUnit,
///     Batch, StockMovement), so these tests cover both halves: fractional
///     quantities working for a weight-sold product, and being rejected for
///     a unit-sold one.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class FractionalQuantityTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreatingAProduct_WithNoUnitOfSaleInThePayload_DefaultsToUnidad()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto sin unitOfSale",
            description = "creado por un test",
            category = "ABARROTES",
            basePrice = 5m
        });

        response.EnsureSuccessStatusCode();
        Assert.Equal("UNIDAD", (await ReadJsonAsync(response)).GetProperty("unitOfSale").GetString());
    }

    [Fact]
    public async Task AProductSoldByWeight_AcceptsAFractionalStockIntakeAndSale()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client, unitOfSale: "PESO");
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 12.5m)).EnsureSuccessStatusCode();
        Assert.Equal(12.5m, await GetTotalStockAsync(client, productId));

        var sale = await CreateSaleAsync(client, SaleLine(productId, quantity: 2.35m, unitPrice: 10m));
        sale.EnsureSuccessStatusCode();

        var saleBody = await ReadJsonAsync(sale);
        var detail = saleBody.GetProperty("details")[0];
        Assert.Equal(2.35m, detail.GetProperty("quantity").GetDecimal());

        Assert.Equal(10.15m, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task AProductSoldByUnit_RejectsAFractionalSaleQuantity()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client); // defaults to UNIDAD
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var response = await CreateSaleAsync(client, SaleLine(productId, quantity: 2.5m, unitPrice: 10m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Stock must be untouched — the whole sale is rejected, not partially applied.
        Assert.Equal(10, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task AProductSoldByUnit_RejectsAFractionalStockIntake()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var response = await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5.5m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task AProductSoldByUnit_RejectsAFractionalStockAdjustment()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment",
            new { warehouseId, delta = -2.5m, reason = "ajuste fraccionario invalido" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(10, await GetTotalStockAsync(client, productId));
    }

    /// <summary>Even a weight-sold product is bounded by what sale_details.quantity (decimal(10,2)) can actually store.</summary>
    [Fact]
    public async Task AQuantityWithMoreThanTwoDecimalPlaces_IsRejectedEvenForAProductSoldByWeight()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client, unitOfSale: "PESO");
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var response = await CreateSaleAsync(client, SaleLine(productId, quantity: 2.567m, unitPrice: 10m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatingAProduct_CanSwitchItToSoldByWeight()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client); // starts as UNIDAD
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var getResponse = await client.GetAsync($"/api/v1/products/{productId}");
        var product = await ReadJsonAsync(getResponse);

        var updateResponse = await client.PatchAsJsonAsync($"/api/v1/products/{productId}", new
        {
            name = product.GetProperty("name").GetString(),
            description = product.GetProperty("description").GetString(),
            category = product.GetProperty("category").GetString(),
            basePrice = product.GetProperty("basePrice").GetDecimal(),
            unitOfSale = "PESO"
        });
        updateResponse.EnsureSuccessStatusCode();
        Assert.Equal("PESO", (await ReadJsonAsync(updateResponse)).GetProperty("unitOfSale").GetString());

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 3.25m)).EnsureSuccessStatusCode();
        Assert.Equal(3.25m, await GetTotalStockAsync(client, productId));
    }
}

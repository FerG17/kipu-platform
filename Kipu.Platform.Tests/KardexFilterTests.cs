using System.Net.Http.Json;
using System.Text.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X6 Kardex: GET /api/v1/stock-movements/filtered — the unpaginated,
///     ascending-capable sibling of GET /api/v1/stock-movements that the
///     Kardex page uses to build a per-product running balance.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class KardexFilterTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private static async Task<int> CreateProductWithCategoryAsync(HttpClient client, string category, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name,
            description = "creado por un test",
            category,
            basePrice = 10m,
            unitOfSale = "UNIDAD"
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Filtered_DefaultsToDescending_LikeTheReportQueryItShares()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        // RegisteredAt is a MySQL `datetime` column (1-second resolution, see
        // the InitialCreate migration) — two intakes issued back-to-back can
        // otherwise tie, making the order between them unspecified.
        (await RegisterStockIntakeAsync(client, productId, warehouseId, 5)).EnsureSuccessStatusCode();
        await Task.Delay(1100);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, 3)).EnsureSuccessStatusCode();

        var movements = await ReadJsonAsync(await client.GetAsync($"/api/v1/stock-movements/filtered?productId={productId}"));

        Assert.Equal(2, movements.GetArrayLength());
        Assert.Equal(3, movements[0].GetProperty("quantity").GetDecimal());
        Assert.Equal(5, movements[1].GetProperty("quantity").GetDecimal());
    }

    [Fact]
    public async Task Filtered_Ascending_ReturnsOldestFirst_ForARunningBalance()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, 5)).EnsureSuccessStatusCode();
        await Task.Delay(1100);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, 3)).EnsureSuccessStatusCode();

        var movements = await ReadJsonAsync(await client.GetAsync(
            $"/api/v1/stock-movements/filtered?productId={productId}&ascending=true"));

        Assert.Equal(2, movements.GetArrayLength());
        Assert.Equal(5, movements[0].GetProperty("quantity").GetDecimal());
        Assert.Equal(3, movements[1].GetProperty("quantity").GetDecimal());
    }

    [Fact]
    public async Task Filtered_ExposesUnitCostFromTheMovementsBatch()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, 5, purchasePrice: 4.25m))
            .EnsureSuccessStatusCode();

        var movements = await ReadJsonAsync(await client.GetAsync($"/api/v1/stock-movements/filtered?productId={productId}"));

        Assert.Equal(4.25m, movements[0].GetProperty("unitCost").GetDecimal());
        Assert.True(movements[0].GetProperty("batchId").GetInt32() > 0);
    }

    [Fact]
    public async Task Filtered_ByCategory_OnlyReturnsMovementsOfProductsInThatCategory()
    {
        var client = await CreateBusinessAsync();
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        var dairyProductId = await CreateProductWithCategoryAsync(client, "DAIRY", "Leche");
        var grainsProductId = await CreateProductWithCategoryAsync(client, "GRAINS", "Arroz");

        (await RegisterStockIntakeAsync(client, dairyProductId, warehouseId, 5)).EnsureSuccessStatusCode();
        (await RegisterStockIntakeAsync(client, grainsProductId, warehouseId, 7)).EnsureSuccessStatusCode();

        var movements = await ReadJsonAsync(await client.GetAsync("/api/v1/stock-movements/filtered?category=DAIRY"));

        Assert.Equal(1, movements.GetArrayLength());
        Assert.Equal(dairyProductId, movements[0].GetProperty("productId").GetInt32());
    }

    [Fact]
    public async Task PaginatedEndpoint_AlsoExposesUnitCost_NowThatBatchIsIncluded()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, 5, purchasePrice: 6.5m))
            .EnsureSuccessStatusCode();

        var envelope = await ReadJsonEnvelopeAsync(await client.GetAsync("/api/v1/stock-movements"));
        var item = envelope.GetProperty("items")[0];

        Assert.Equal(6.5m, item.GetProperty("unitCost").GetDecimal());
    }
}

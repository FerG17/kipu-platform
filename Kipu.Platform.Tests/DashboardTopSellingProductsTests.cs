using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>X6 #6: the dashboard's top-products widget ranks by units actually sold, not by current stock on hand.</summary>
[Collection(KipuApiCollection.Name)]
public class DashboardTopSellingProductsTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task RanksBySold_NotByRemainingStock()
    {
        var client = await CreateBusinessAsync();
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        // Producto A: mucho stock, pero nunca se vendió.
        var productAId = await CreateProductAsync(client, name: "Producto Mucho Stock");
        (await RegisterStockIntakeAsync(client, productAId, warehouseId, quantity: 100, minimumStock: 1))
            .EnsureSuccessStatusCode();

        // Producto B: poco stock inicial, pero se vendió varias veces.
        var productBId = await CreateProductAsync(client, basePrice: 5m, name: "Producto Más Vendido");
        (await RegisterStockIntakeAsync(client, productBId, warehouseId, quantity: 10, minimumStock: 1))
            .EnsureSuccessStatusCode();
        (await CreateSaleAsync(client, SaleLine(productBId, 2, 5m))).EnsureSuccessStatusCode();
        (await CreateSaleAsync(client, SaleLine(productBId, 3, 5m))).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/v1/dashboard/top-selling-products?count=5");
        response.EnsureSuccessStatusCode();
        var topSelling = await response.Content.ReadFromJsonAsync<List<TopSellingProductDto>>();

        Assert.NotNull(topSelling);
        Assert.Single(topSelling);
        Assert.Equal(productBId, topSelling[0].ProductId);
        Assert.Equal(5m, topSelling[0].TotalSold);
    }

    private record TopSellingProductDto(int ProductId, string ProductName, decimal TotalSold);
}

using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     Covers the 2026-08-18 audit finding (I9): "stock health" mixed units
///     — TotalProducts counts distinct products, but LowStockCount counted
///     InventoryItem rows, one per (product, warehouse). A single product
///     split across 2+ warehouses could count as "low stock" more than
///     once, inflating LowStockCount past what TotalProducts could ever
///     support and skewing the health percentage derived from both.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class DashboardKpisTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task LowStockCount_CountsOncePerProduct_EvenWhenSplitAcrossWarehouses()
    {
        var client = await CreateBusinessAsync();
        var mainWarehouseId = await GetDefaultWarehouseIdAsync(client);

        var secondWarehouseResponse = await client.PostAsJsonAsync("/api/v1/warehouses", new
        {
            name = "Almacén Secundario",
            code = "ALM-002",
            address = "",
            capacity = "MEDIUM"
        });
        secondWarehouseResponse.EnsureSuccessStatusCode();
        var secondWarehouseId = (await ReadJsonAsync(secondWarehouseResponse)).GetProperty("id").GetInt32();

        // Product A: 1 unit in each of 2 warehouses (2 total), minimumStock
        // 5 on both — low stock at the product level (2 <= 5), but two
        // separate InventoryItem rows are individually "low" too.
        var productAId = await CreateProductAsync(client, name: "Producto Bajo Stock");
        (await RegisterStockIntakeAsync(client, productAId, mainWarehouseId, quantity: 1, minimumStock: 5))
            .EnsureSuccessStatusCode();
        (await RegisterStockIntakeAsync(client, productAId, secondWarehouseId, quantity: 1, minimumStock: 5))
            .EnsureSuccessStatusCode();

        // Product B: healthy stock, not low.
        var productBId = await CreateProductAsync(client, name: "Producto Stock Sano");
        (await RegisterStockIntakeAsync(client, productBId, mainWarehouseId, quantity: 100, minimumStock: 5))
            .EnsureSuccessStatusCode();

        var kpisResponse = await client.GetAsync("/api/v1/dashboard/kpis");
        kpisResponse.EnsureSuccessStatusCode();
        var kpis = await ReadJsonAsync(kpisResponse);

        Assert.Equal(2, kpis.GetProperty("totalProducts").GetInt32());
        // The old bug reported 2 here (one per warehouse row) instead of 1
        // (one per product) — LowStockCount can never legitimately exceed
        // TotalProducts once both are counted in the same unit.
        Assert.Equal(1, kpis.GetProperty("lowStockCount").GetInt32());
        Assert.Equal(50.0, kpis.GetProperty("stockHealthPercentage").GetDouble(), precision: 5);
    }

    /// <summary>
    ///     A deactivated product must not count against catalog health or be
    ///     valorized as sellable inventory. Reaching "inactive with stock"
    ///     through the real API is no longer possible at all as of I26
    ///     (RegisterStockIntake now rejects a deactivated product, and
    ///     DeleteProduct already refused to deactivate one that still has
    ///     stock — see ProductLifecycleTests.RegisterStockIntake_ForADeactivatedProduct_IsRejected),
    ///     so this only exercises the reachable case: a product deactivated
    ///     at 0 stock must not appear in any KPI, including its (unrelated,
    ///     deliberately high) BasePrice being valorized into InventoryValue.
    ///     The facade's own inventory-item filter (GetProductKpisSnapshot)
    ///     stays as defense-in-depth for any pre-existing data from before
    ///     this fix.
    /// </summary>
    [Fact]
    public async Task InventoryValueAndLowStockCount_ExcludeDeactivatedProducts()
    {
        var client = await CreateBusinessAsync();
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var activeProductId = await CreateProductAsync(client, basePrice: 10m, name: "Producto Activo");
        (await RegisterStockIntakeAsync(client, activeProductId, warehouseId, quantity: 5, minimumStock: 1))
            .EnsureSuccessStatusCode();

        var inactiveProductId = await CreateProductAsync(client, basePrice: 1000m, name: "Producto Desactivado");
        (await client.DeleteAsync($"/api/v1/products/{inactiveProductId}")).EnsureSuccessStatusCode();

        var kpisResponse = await client.GetAsync("/api/v1/dashboard/kpis");
        kpisResponse.EnsureSuccessStatusCode();
        var kpis = await ReadJsonAsync(kpisResponse);

        Assert.Equal(1, kpis.GetProperty("totalProducts").GetInt32());
        Assert.Equal(0, kpis.GetProperty("lowStockCount").GetInt32());
        Assert.Equal(50m, kpis.GetProperty("inventoryValue").GetDecimal());
    }
}

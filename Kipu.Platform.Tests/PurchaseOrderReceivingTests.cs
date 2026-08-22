using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     Covers a 2026-08-19 manual-QA finding: receiving a purchase order
///     always deposited into the business's first warehouse, regardless of
///     where the product's existing stock actually lived. For a product
///     already stocked in a warehouse other than the first, that fragmented
///     its stock into two separate InventoryItem rows — one real, one
///     freshly created by the receive — each independently evaluated for
///     LOW_STOCK/OUT_OF_STOCK, so the original alert never cleared while a
///     second, spurious one fired for the "new" empty-looking row.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class PurchaseOrderReceivingTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ReceivingAnOrder_ForAProductAlreadyStockedInASecondWarehouse_TopsUpTheSameInventoryItem()
    {
        var client = await CreateBusinessAsync();

        var secondWarehouseResponse = await client.PostAsJsonAsync("/api/v1/warehouses", new
        {
            name = "Almacén Secundario",
            code = "ALM-002",
            address = "",
            capacity = "MEDIUM"
        });
        secondWarehouseResponse.EnsureSuccessStatusCode();
        var secondWarehouseId = (await ReadJsonAsync(secondWarehouseResponse)).GetProperty("id").GetInt32();

        var productId = await CreateProductAsync(client, name: "Fanta");
        // All of this product's real stock lives in the SECOND warehouse —
        // the first ("Almacén Principal") has none of it at all.
        (await RegisterStockIntakeAsync(client, productId, secondWarehouseId, quantity: 46, minimumStock: 100))
            .EnsureSuccessStatusCode();

        var supplierId = await CreateSupplierAsync(client);
        var orderResponse = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 100, unitPrice: 5m);
        orderResponse.EnsureSuccessStatusCode();
        var orderId = (await ReadJsonAsync(orderResponse)).GetProperty("id").GetInt32();

        (await client.PatchAsJsonAsync($"/api/v1/purchases/{orderId}", new { status = "RECEIVED" })).EnsureSuccessStatusCode();

        var inventoryResponse = await client.GetAsync($"/api/v1/inventories?productId={productId}");
        inventoryResponse.EnsureSuccessStatusCode();
        var items = (await ReadJsonAsync(inventoryResponse)).EnumerateArray().ToList();

        Assert.Single(items);
        Assert.Equal(146, items[0].GetProperty("stockUnit").GetDecimal());
        Assert.Equal(secondWarehouseId, items[0].GetProperty("warehouseId").GetInt32());

        var alertsResponse = await client.GetAsync("/api/v1/alerts");
        alertsResponse.EnsureSuccessStatusCode();
        var activeAlerts = (await ReadJsonAsync(alertsResponse)).EnumerateArray()
            .Where(alert => alert.GetProperty("productId").GetInt32() == productId
                && (alert.GetProperty("type").GetString() == "LOW_STOCK" || alert.GetProperty("type").GetString() == "OUT_OF_STOCK"))
            .ToList();

        Assert.Empty(activeAlerts);
    }
}

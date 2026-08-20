using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>X4 Bloque 2: inventory/batch integrity fixes (A7, A8, M9, M10, M12).</summary>
[Collection(KipuApiCollection.Name)]
public class InventoryIntegrityTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>A7: registering an intake that only sets an expiration date must not zero out a batch's existing purchase price.</summary>
    [Fact]
    public async Task StockIntake_WithOnlyExpiration_PreservesTheBatchsExistingPurchasePrice()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, purchasePrice: 8.5m))
            .EnsureSuccessStatusCode();

        var withoutPrice = await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 2,
            expiration: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30));
        withoutPrice.EnsureSuccessStatusCode();

        var batches = await client.GetAsync($"/api/v1/batches?productId={productId}");
        batches.EnsureSuccessStatusCode();
        var batch = (await ReadJsonAsync(batches)).EnumerateArray().First();
        Assert.Equal(8.5m, batch.GetProperty("purchasePrice").GetDecimal());
    }

    /// <summary>A8: reactivating a product whose barcode was taken by another active product while it was deactivated must be rejected, not 500.</summary>
    [Fact]
    public async Task ReactivatingAProduct_WhoseBarcodeWasReusedWhileInactive_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var firstResponse = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto original", description = "d", category = "ABARROTES", basePrice = 10m, barcode = "7501234567890"
        });
        firstResponse.EnsureSuccessStatusCode();
        var firstId = (await ReadJsonAsync(firstResponse)).GetProperty("id").GetInt32();

        (await client.DeleteAsync($"/api/v1/products/{firstId}")).EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto nuevo", description = "d", category = "ABARROTES", basePrice = 12m, barcode = "7501234567890"
        });
        secondResponse.EnsureSuccessStatusCode();

        var reactivate = await client.PostAsync($"/api/v1/products/{firstId}/activate", null);
        Assert.Equal(HttpStatusCode.Conflict, reactivate.StatusCode);
    }

    /// <summary>A8, the happy path: reactivating a product whose barcode is free must still work.</summary>
    [Fact]
    public async Task ReactivatingAProduct_WithNoBarcodeConflict_Succeeds()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        (await client.DeleteAsync($"/api/v1/products/{productId}")).EnsureSuccessStatusCode();

        var reactivate = await client.PostAsync($"/api/v1/products/{productId}/activate", null);
        reactivate.EnsureSuccessStatusCode();
    }

    /// <summary>M9: an absurdly large stock intake quantity must be rejected, not accepted toward a future overflow.</summary>
    [Fact]
    public async Task StockIntake_WithAnImplausibleQuantity_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var response = await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5_000_000);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>M9: an oversized supplier name on a stock intake must be rejected, not 500 from MySQL.</summary>
    [Fact]
    public async Task StockIntake_WithAnOversizedSupplierName_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var response = await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 1,
            supplier: new string('a', 200));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>M10: a deactivated warehouse must not accept a new stock intake.</summary>
    [Fact]
    public async Task StockIntake_IntoADeactivatedWarehouse_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        (await client.PatchAsJsonAsync($"/api/v1/warehouses/{warehouseId}", new
        {
            name = "Almacén Principal", code = "ALM-001", address = "", capacity = "MEDIUM", active = false
        })).EnsureSuccessStatusCode();

        var response = await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>M10: a manual stock adjustment against a deactivated warehouse must also be rejected.</summary>
    [Fact]
    public async Task StockAdjustment_AgainstADeactivatedWarehouse_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        (await client.PatchAsJsonAsync($"/api/v1/warehouses/{warehouseId}", new
        {
            name = "Almacén Principal", code = "ALM-001", address = "", capacity = "MEDIUM", active = false
        })).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment", new
        {
            warehouseId, delta = -1, reason = "conteo físico"
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>M12: creating a product must not leave it committed without its supplier tags if the second write fails — verified via the happy path staying atomic (both land together).</summary>
    [Fact]
    public async Task CreatingAProduct_WithSuppliers_PersistsBothTogether()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Producto con proveedor", description = "d", category = "ABARROTES", basePrice = 10m,
            supplierIds = new[] { supplierId }
        });
        response.EnsureSuccessStatusCode();

        var body = await ReadJsonAsync(response);
        var supplierIds = body.GetProperty("supplierIds").EnumerateArray().Select(id => id.GetInt32()).ToList();
        Assert.Contains(supplierId, supplierIds);
    }
}

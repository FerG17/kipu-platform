using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X3 "Endurecer tests": role-denial coverage for BatchesController,
///     InventoriesController, StockMovementsController and
///     WarehousesController — none had any before this file.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class InventoryAuthorizationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>
    ///     X5 Bloque C removed the standalone POST /batches endpoint — a
    ///     batch is now only ever created as part of a stock intake (which
    ///     always carries a real Quantity), so that's the role-denial this
    ///     test now covers instead.
    /// </summary>
    [Fact]
    public async Task RegisteringAStockIntakeWithABatch_AsCashier_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var productId = await CreateProductAsync(admin);
        var warehouseId = await GetDefaultWarehouseIdAsync(admin);
        var cashier = await InviteAndSignInAsync(admin, CashierRoleId);

        var response = await RegisterStockIntakeAsync(cashier, productId, warehouseId, quantity: 5,
            expiration: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), purchasePrice: 5m);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DiscardingABatch_AsCashier_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var productId = await CreateProductAsync(admin);
        var warehouseId = await GetDefaultWarehouseIdAsync(admin);
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        (await RegisterStockIntakeAsync(admin, productId, warehouseId, quantity: 5, expiration: expiration))
            .EnsureSuccessStatusCode();

        var batches = await ReadJsonAsync(await admin.GetAsync($"/api/v1/batches?productId={productId}"));
        var batchId = batches.EnumerateArray().First().GetProperty("id").GetInt32();

        var cashier = await InviteAndSignInAsync(admin, CashierRoleId);
        var response = await cashier.PostAsync($"/api/v1/batches/{batchId}/discard", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdatingMinimumStock_AsCashier_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var productId = await CreateProductAsync(admin);
        var cashier = await InviteAndSignInAsync(admin, CashierRoleId);

        var response = await cashier.PatchAsJsonAsync($"/api/v1/inventories/{productId}/minimum-stock",
            new { minimumStock = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdjustingStock_AsCashier_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var productId = await CreateProductAsync(admin);
        var warehouseId = await GetDefaultWarehouseIdAsync(admin);
        var cashier = await InviteAndSignInAsync(admin, CashierRoleId);

        var response = await cashier.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment",
            new { warehouseId, delta = 1, reason = "conteo" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListingStockMovements_AsCashier_IsForbidden()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        var response = await cashier.GetAsync("/api/v1/stock-movements");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatingOrUpdatingAWarehouse_AsCashier_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var cashier = await InviteAndSignInAsync(admin, CashierRoleId);

        var createResponse = await cashier.PostAsJsonAsync("/api/v1/warehouses",
            new { name = "Sucursal", code = "SUC-1", address = "Av. Test 123", capacity = "MEDIUM" });
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);

        var warehouseId = await GetDefaultWarehouseIdAsync(admin);
        var updateResponse = await cashier.PatchAsJsonAsync($"/api/v1/warehouses/{warehouseId}",
            new { name = "Renombrado", code = "SUC-1", address = "Av. Test 123", capacity = "MEDIUM", active = true });
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }
}

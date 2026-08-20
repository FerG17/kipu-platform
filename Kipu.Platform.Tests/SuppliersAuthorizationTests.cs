using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X3 "Endurecer tests": role-denial coverage for SuppliersController,
///     PurchasesController and PurchaseDetailsController — none had any
///     before this file. Each is gated by one uniform class-level
///     [Authorize(Admin, Warehouse)], so one denial test per controller
///     (as Cashier) covers every one of its actions.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class SuppliersAuthorizationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ListingSuppliers_AsCashier_IsForbidden()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        var response = await cashier.GetAsync("/api/v1/suppliers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListingPurchaseOrders_AsCashier_IsForbidden()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        var response = await cashier.GetAsync("/api/v1/purchases");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListingPurchaseDetails_AsCashier_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var productId = await CreateProductAsync(admin);
        var supplierId = await CreateSupplierAsync(admin);
        var order = await CreatePurchaseOrderAsync(admin, supplierId, productId, quantity: 5);
        order.EnsureSuccessStatusCode();
        var purchaseId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        var cashier = await InviteAndSignInAsync(admin, CashierRoleId);
        var response = await cashier.GetAsync($"/api/v1/purchase-details?purchaseId={purchaseId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

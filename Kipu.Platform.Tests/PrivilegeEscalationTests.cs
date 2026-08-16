using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     The insider threat: a real employee of the bodega, holding a real
///     token, trying to do something their role isn't entitled to — a cashier
///     reading the owner's financial reports, promoting themselves to admin,
///     or reading a colleague's account.
///
///     Every one of these must be a 403, not merely "the UI doesn't show the
///     button". The role matrix documented on AuthorizeAttribute is only real
///     if the server enforces it.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class PrivilegeEscalationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private static void AssertForbidden(HttpResponseMessage response, string what)
    {
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden,
            $"{what} must be refused with 403, got {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    public async Task Cashier_CannotListTheTeam()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);
        AssertForbidden(await cashier.GetAsync("/api/v1/users"), "a cashier listing the team");
    }

    /// <summary>The escalation that matters most: a cashier minting themselves an ADMIN colleague.</summary>
    [Fact]
    public async Task Cashier_CannotInviteUsers()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        var response = await cashier.PostAsJsonAsync("/api/v1/users", new
        {
            email = $"escalated-{Guid.NewGuid():N}@test.local",
            password = ValidPassword,
            name = "Mallory",
            lastName = "Admin",
            roleId = AdminRoleId,
            phone = ""
        });

        AssertForbidden(response, "a cashier inviting an admin");
    }

    [Fact]
    public async Task Cashier_CannotDeleteUsers()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var cashier = await InviteAndSignInAsync(admin.Client, CashierRoleId);

        AssertForbidden(await cashier.DeleteAsync($"/api/v1/users/{admin.UserId}"), "a cashier deleting the owner");
    }

    [Fact]
    public async Task Cashier_CannotReadFinancialDashboardOrReports()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        AssertForbidden(await cashier.GetAsync("/api/v1/dashboard/kpis"), "a cashier reading business KPIs");
        AssertForbidden(await cashier.GetAsync("/api/v1/dashboard/sales-by-day"), "a cashier reading sales by day");
        AssertForbidden(await cashier.GetAsync("/api/v1/reports"), "a cashier listing reports");
    }

    [Fact]
    public async Task Cashier_CannotTouchTheProductCatalog()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        AssertForbidden(await CreateProductResponseAsync(cashier), "a cashier creating a product");
        AssertForbidden(await cashier.DeleteAsync("/api/v1/products/1"), "a cashier deleting a product");
    }

    [Fact]
    public async Task Warehouse_CannotSellOrSeeCustomers()
    {
        var warehouse = await InviteAndSignInAsync(await CreateBusinessAsync(), WarehouseRoleId);

        AssertForbidden(await CreateSaleAsync(warehouse, SaleLine(1, 1, 1m)), "a warehouse user creating a sale");
        AssertForbidden(await warehouse.GetAsync("/api/v1/sales"), "a warehouse user listing sales");
        AssertForbidden(await warehouse.GetAsync("/api/v1/customers"), "a warehouse user listing customers");
        AssertForbidden(await warehouse.GetAsync("/api/v1/payment-plans/pending"), "a warehouse user reading credit");
    }

    [Fact]
    public async Task Warehouse_CannotReadFinancialReports()
    {
        var warehouse = await InviteAndSignInAsync(await CreateBusinessAsync(), WarehouseRoleId);

        AssertForbidden(await warehouse.GetAsync("/api/v1/dashboard/kpis"), "a warehouse user reading KPIs");
        AssertForbidden(await warehouse.GetAsync("/api/v1/reports"), "a warehouse user listing reports");
    }

    [Fact]
    public async Task Cashier_CannotConfigureAlertRules()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        var response = await cashier.PostAsJsonAsync("/api/v1/alert-rules",
            new { alertType = "LOW_STOCK", thresholdValue = 0, enabled = false });

        AssertForbidden(response, "a cashier silencing the alert engine");
    }

    /// <summary>A colleague's profile is not the caller's to read, even inside the same bodega.</summary>
    [Fact]
    public async Task Cashier_CannotReadAnotherEmployeesAccount()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var cashier = await InviteAndSignInAsync(admin.Client, CashierRoleId);

        AssertForbidden(await cashier.GetAsync($"/api/v1/users/{admin.UserId}"), "a cashier reading the owner's account");
    }

    [Fact]
    public async Task Cashier_CannotChangeAnotherEmployeesPassword()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var cashier = await InviteAndSignInAsync(admin.Client, CashierRoleId);

        var response = await cashier.PostAsJsonAsync($"/api/v1/users/{admin.UserId}/change-password", new
        {
            currentPassword = ValidPassword,
            newPassword = "Attack3rPassword!"
        });

        AssertForbidden(response, "a cashier changing the owner's password");
    }

    [Fact]
    public async Task Cashier_CannotEditAnotherEmployeesProfile()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var cashier = await InviteAndSignInAsync(admin.Client, CashierRoleId);

        var response = await cashier.PatchAsJsonAsync($"/api/v1/users/{admin.UserId}",
            new { name = "Mallory", lastName = "Owner", phone = "" });

        AssertForbidden(response, "a cashier editing the owner's profile");
    }

    [Fact]
    public async Task Cashier_CannotEditTheBusinessProfile()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var cashier = await InviteAndSignInAsync(admin.Client, CashierRoleId);

        var response = await cashier.PatchAsJsonAsync($"/api/v1/businesses/{admin.BusinessId}",
            new { name = "Kipu de Mallory", type = "RETAIL", address = "", ruc = "" });

        AssertForbidden(response, "a cashier renaming the business");
    }

    /// <summary>Self-service still has to work — these are the actions each role legitimately owns.</summary>
    [Fact]
    public async Task Employees_CanStillDoTheirOwnJob()
    {
        var admin = await CreateBusinessAsync();
        var cashier = await InviteAndSignInAsync(admin, CashierRoleId);
        var warehouse = await InviteAndSignInAsync(admin, WarehouseRoleId);

        // Warehouse stocks the shelf.
        var productId = await CreateProductAsync(warehouse);
        var warehouseId = await GetDefaultWarehouseIdAsync(warehouse);
        (await RegisterStockIntakeAsync(warehouse, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        // Cashier sells from it.
        (await CreateSaleAsync(cashier, SaleLine(productId, quantity: 1, unitPrice: 10m))).EnsureSuccessStatusCode();

        // And both can read the catalog they work with.
        (await cashier.GetAsync("/api/v1/products")).EnsureSuccessStatusCode();
        (await warehouse.GetAsync("/api/v1/products")).EnsureSuccessStatusCode();
    }
}

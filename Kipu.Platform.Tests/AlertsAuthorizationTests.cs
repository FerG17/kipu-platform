using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X3 "Endurecer tests": role-denial coverage for AlertsController and
///     AlertRulesController — neither controller had any before this file
///     (AlertRulesController had exactly one, PrivilegeEscalationTests'
///     Cashier_CannotConfigureAlertRules, covering only POST as Cashier).
/// </summary>
[Collection(KipuApiCollection.Name)]
public class AlertsAuthorizationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreatingAnAlert_AsCashier_IsForbidden()
    {
        var cashier = await InviteAndSignInAsync(await CreateBusinessAsync(), CashierRoleId);

        var response = await cashier.PostAsJsonAsync("/api/v1/alerts", new
        {
            productId = 0, batchId = (int?)null, productName = "x", type = "LOW_STOCK", severity = "LOW",
            message = "x", currentStock = 0, minStock = 0, daysToExpiry = (int?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatingAnAlert_AsWarehouse_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var warehouseRole = await InviteAndSignInAsync(admin, WarehouseRoleId);

        var response = await warehouseRole.PostAsJsonAsync("/api/v1/alerts", new
        {
            productId = 0, batchId = (int?)null, productName = "x", type = "LOW_STOCK", severity = "LOW",
            message = "x", currentStock = 0, minStock = 0, daysToExpiry = (int?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AcknowledgingOrResolvingAnAlert_AsCashier_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var alertId = await CreateOutOfStockAlertAsync(admin);
        var cashier = await InviteAndSignInAsync(admin, CashierRoleId);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await cashier.PostAsync($"/api/v1/alerts/{alertId}/acknowledge", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await cashier.PostAsync($"/api/v1/alerts/{alertId}/resolve", null)).StatusCode);
    }

    [Fact]
    public async Task ConfiguringAnAlertRule_AsWarehouse_IsForbidden()
    {
        var admin = await CreateBusinessAsync();
        var warehouseRole = await InviteAndSignInAsync(admin, WarehouseRoleId);

        var response = await warehouseRole.PostAsJsonAsync("/api/v1/alert-rules",
            new { alertType = "LOW_STOCK", thresholdValue = 0, enabled = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Selling a product down to zero raises an OUT_OF_STOCK alert through the reactive engine.</summary>
    private static async Task<int> CreateOutOfStockAlertAsync(HttpClient client)
    {
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 2)).EnsureSuccessStatusCode();
        (await CreateSaleAsync(client, SaleLine(productId, quantity: 2, unitPrice: 10m))).EnsureSuccessStatusCode();

        var alerts = await client.GetAsync("/api/v1/alerts");
        alerts.EnsureSuccessStatusCode();

        var rows = (await ReadJsonAsync(alerts)).EnumerateArray().ToList();
        Assert.NotEmpty(rows);
        return rows[0].GetProperty("id").GetInt32();
    }
}

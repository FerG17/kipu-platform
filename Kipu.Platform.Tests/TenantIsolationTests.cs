using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     Adversarial multi-tenancy suite: every test plays the part of a real
///     tenant ("attacker") holding a perfectly valid token for its own
///     business, and then aims it at another business's rows by guessing ids.
///
///     This is the single highest-consequence failure mode for this product —
///     one bodega reading or moving another bodega's stock, sales, customers
///     or credit is a total breach — so it is tested resource by resource
///     rather than trusting that AppDbContext's global query filter covers
///     everything. Anything that answers 2xx here is a live IDOR.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class TenantIsolationTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>Two independent businesses: the victim, and the attacker who will aim at the victim's ids.</summary>
    private async Task<(HttpClient Victim, HttpClient Attacker)> TwoBusinessesAsync()
    {
        return (await CreateBusinessAsync(), await CreateBusinessAsync());
    }

    private static void AssertDenied(HttpResponseMessage response, string what)
    {
        Assert.False(response.IsSuccessStatusCode,
            $"cross-tenant access to {what} must be denied, got {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    public async Task Product_OfAnotherBusiness_IsNotReadable()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var productId = await CreateProductAsync(victim);

        AssertDenied(await attacker.GetAsync($"/api/v1/products/{productId}"), "a product");
    }

    [Fact]
    public async Task Product_OfAnotherBusiness_CannotBeUpdated()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var productId = await CreateProductAsync(victim);

        var response = await attacker.PatchAsJsonAsync($"/api/v1/products/{productId}", new
        {
            name = "secuestrado", description = "", category = "ABARROTES", basePrice = 1m
        });

        AssertDenied(response, "a product update");

        // And the victim's row must be untouched.
        var reread = await victim.GetAsync($"/api/v1/products/{productId}");
        reread.EnsureSuccessStatusCode();
        Assert.Equal("Producto de prueba", (await ReadJsonAsync(reread)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Product_OfAnotherBusiness_CannotBeDeleted()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var productId = await CreateProductAsync(victim);

        AssertDenied(await attacker.DeleteAsync($"/api/v1/products/{productId}"), "a product deletion");
    }

    [Fact]
    public async Task StockIntake_OnAnotherBusinessProduct_IsRejected()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var productId = await CreateProductAsync(victim);
        var attackerWarehouseId = await GetDefaultWarehouseIdAsync(attacker);

        AssertDenied(await RegisterStockIntakeAsync(attacker, productId, attackerWarehouseId, quantity: 50),
            "a stock intake on another business's product");
    }

    [Fact]
    public async Task Sale_OfAnotherBusinessProduct_IsRejected()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var productId = await CreateProductAsync(victim);
        var warehouseId = await GetDefaultWarehouseIdAsync(victim);
        (await RegisterStockIntakeAsync(victim, productId, warehouseId, quantity: 100)).EnsureSuccessStatusCode();

        AssertDenied(await CreateSaleAsync(attacker, SaleLine(productId, quantity: 1, unitPrice: 1m)),
            "selling another business's product");

        // The victim's stock must not have moved.
        Assert.Equal(100, await GetTotalStockAsync(victim, productId));
    }

    [Fact]
    public async Task Sale_OfAnotherBusiness_IsNotReadable()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var saleId = await CreateSoldSaleAsync(victim);

        AssertDenied(await attacker.GetAsync($"/api/v1/sales/{saleId}"), "a sale");
    }

    /// <summary>
    ///     SaleDetail carries no BusinessId of its own, so it gets no global
    ///     query filter — its protection is entirely inherited from the parent
    ///     Sale lookup. Worth its own test precisely because of that.
    /// </summary>
    [Fact]
    public async Task SaleDetails_OfAnotherBusiness_AreNotReadable()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var saleId = await CreateSoldSaleAsync(victim);

        AssertDenied(await attacker.GetAsync($"/api/v1/sale-details?saleId={saleId}"), "another business's sale lines");
    }

    [Fact]
    public async Task Sale_OfAnotherBusiness_CannotBeCancelled()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var saleId = await CreateSoldSaleAsync(victim);

        var response = await attacker.PatchAsJsonAsync($"/api/v1/sales/{saleId}", new { status = "CANCELLED" });
        AssertDenied(response, "cancelling another business's sale");

        var reread = await victim.GetAsync($"/api/v1/sales/{saleId}");
        reread.EnsureSuccessStatusCode();
        Assert.NotEqual("CANCELLED", (await ReadJsonAsync(reread)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Business_OfAnotherTenant_IsNotReadable()
    {
        var victim = await CreateBusinessWithOwnerAsync();
        var attacker = await CreateBusinessAsync();

        AssertDenied(await attacker.GetAsync($"/api/v1/businesses/{victim.BusinessId}"), "another tenant's business profile");
    }

    [Fact]
    public async Task Business_OfAnotherTenant_CannotBeUpdated()
    {
        var victim = await CreateBusinessWithOwnerAsync();
        var attacker = await CreateBusinessAsync();

        var response = await attacker.PatchAsJsonAsync($"/api/v1/businesses/{victim.BusinessId}", new
        {
            name = "secuestrada", type = "RETAIL", address = "", ruc = ""
        });

        AssertDenied(response, "another tenant's business profile update");
    }

    [Fact]
    public async Task User_OfAnotherBusiness_IsNotReadable()
    {
        var victim = await CreateBusinessWithOwnerAsync();
        var attacker = await CreateBusinessAsync();

        AssertDenied(await attacker.GetAsync($"/api/v1/users/{victim.UserId}"), "another business's user");
    }

    [Fact]
    public async Task User_OfAnotherBusiness_CannotBeDeleted()
    {
        var victim = await CreateBusinessWithOwnerAsync();
        var attacker = await CreateBusinessAsync();

        AssertDenied(await attacker.DeleteAsync($"/api/v1/users/{victim.UserId}"), "deleting another business's user");
    }

    [Fact]
    public async Task Warehouse_OfAnotherBusiness_IsNotReadable()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var warehouseId = await GetDefaultWarehouseIdAsync(victim);

        AssertDenied(await attacker.GetAsync($"/api/v1/warehouses/{warehouseId}"), "another business's warehouse");
    }

    [Fact]
    public async Task Warehouse_OfAnotherBusiness_CannotBeUpdated()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var warehouseId = await GetDefaultWarehouseIdAsync(victim);

        var response = await attacker.PatchAsJsonAsync($"/api/v1/warehouses/{warehouseId}", new
        {
            name = "secuestrado", code = "HACK", address = "", capacity = "MEDIUM"
        });

        AssertDenied(response, "another business's warehouse update");
    }

    [Fact]
    public async Task Customer_OfAnotherBusiness_IsNotReadable()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var customerId = await CreateCustomerAsync(victim);

        AssertDenied(await attacker.GetAsync($"/api/v1/customers/{customerId}"), "another business's customer");
    }

    [Fact]
    public async Task Customer_OfAnotherBusiness_CannotBeDeleted()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var customerId = await CreateCustomerAsync(victim);

        AssertDenied(await attacker.DeleteAsync($"/api/v1/customers/{customerId}"), "deleting another business's customer");
    }

    [Fact]
    public async Task Supplier_OfAnotherBusiness_IsNotReadable()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var supplierId = await CreateSupplierAsync(victim);

        AssertDenied(await attacker.GetAsync($"/api/v1/suppliers/{supplierId}"), "another business's supplier");
    }

    /// <summary>
    ///     PurchaseOrderDetail, like SaleDetail, has no BusinessId and so no
    ///     query filter of its own — its protection is inherited from the
    ///     parent order lookup.
    /// </summary>
    [Fact]
    public async Task PurchaseDetails_OfAnotherBusiness_AreNotReadable()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var purchaseId = await CreatePendingPurchaseOrderAsync(victim);

        AssertDenied(await attacker.GetAsync($"/api/v1/purchase-details?purchaseId={purchaseId}"),
            "another business's purchase order lines");
    }

    [Fact]
    public async Task PurchaseOrder_OfAnotherBusiness_CannotBeReceived()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var purchaseId = await CreatePendingPurchaseOrderAsync(victim);

        var response = await attacker.PatchAsJsonAsync($"/api/v1/purchases/{purchaseId}", new { status = "RECEIVED" });
        AssertDenied(response, "receiving another business's purchase order");
    }

    [Fact]
    public async Task PaymentPlan_CannotBeAttachedToAnotherBusinessSale()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var saleId = await CreateSoldSaleAsync(victim);

        var schedule = new[] { new { dueDate = "2026-09-15", amount = 10m } };
        var response = await attacker.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, schedule });
        AssertDenied(response, "attaching a payment plan to another business's sale");
    }

    /// <summary>X6 #12 (Bloque G2), Suppliers-side mirror of PaymentPlan_CannotBeAttachedToAnotherBusinessSale.</summary>
    [Fact]
    public async Task SupplierPaymentPlan_CannotBeAttachedToAnotherBusinessPurchaseOrder()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var purchaseOrderId = await CreatePendingPurchaseOrderAsync(victim);

        var schedule = new[] { new { dueDate = "2026-09-15", amount = 10m } };
        var response = await attacker.PostAsJsonAsync("/api/v1/supplier-payment-plans", new { purchaseOrderId, schedule });
        AssertDenied(response, "attaching a payment plan to another business's purchase order");
    }

    [Fact]
    public async Task Report_OfAnotherBusiness_CannotBeExported()
    {
        var (victim, attacker) = await TwoBusinessesAsync();

        var generated = await victim.PostAsJsonAsync("/api/v1/reports", new
        {
            type = "STOCK_MOVEMENTS", dateFrom = (DateOnly?)null, dateTo = (DateOnly?)null
        });
        generated.EnsureSuccessStatusCode();
        var reportId = (await ReadJsonAsync(generated)).GetProperty("id").GetInt32();

        AssertDenied(await attacker.GetAsync($"/api/v1/reports/{reportId}/export"), "another business's report (CSV)");
        AssertDenied(await attacker.GetAsync($"/api/v1/reports/{reportId}/export/pdf"), "another business's report (PDF)");
    }

    [Fact]
    public async Task Batch_OfAnotherBusiness_CannotBeDiscarded()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var productId = await CreateProductAsync(victim);
        var warehouseId = await GetDefaultWarehouseIdAsync(victim);
        (await RegisterStockIntakeAsync(victim, productId, warehouseId, quantity: 5,
            expiration: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))).EnsureSuccessStatusCode();

        var batches = await victim.GetAsync($"/api/v1/batches?productId={productId}");
        batches.EnsureSuccessStatusCode();
        var batchId = (await ReadJsonAsync(batches))[0].GetProperty("id").GetInt32();

        AssertDenied(await attacker.PostAsync($"/api/v1/batches/{batchId}/discard", null),
            "discarding another business's batch");
    }

    [Fact]
    public async Task MinimumStock_OfAnotherBusinessProduct_CannotBeUpdated()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var productId = await CreateProductAsync(victim);
        var warehouseId = await GetDefaultWarehouseIdAsync(victim);
        (await RegisterStockIntakeAsync(victim, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        var response = await attacker.PatchAsJsonAsync($"/api/v1/inventories/{productId}/minimum-stock",
            new { minimumStock = 9999 });

        AssertDenied(response, "another business's minimum-stock threshold");
    }

    [Fact]
    public async Task Alert_OfAnotherBusiness_CannotBeResolved()
    {
        var (victim, attacker) = await TwoBusinessesAsync();
        var alertId = await CreateOutOfStockAlertAsync(victim);

        AssertDenied(await attacker.PostAsync($"/api/v1/alerts/{alertId}/resolve", null),
            "resolving another business's alert");
        AssertDenied(await attacker.PostAsync($"/api/v1/alerts/{alertId}/acknowledge", null),
            "acknowledging another business's alert");
    }

    /// <summary>
    ///     The collection endpoints must never spill across tenants either —
    ///     a brand-new business has to start empty no matter how much data
    ///     every other business in the same database holds.
    /// </summary>
    [Fact]
    public async Task ListEndpoints_NeverReturnAnotherBusinessRows()
    {
        var victim = await CreateBusinessAsync();
        var productId = await CreateProductAsync(victim);
        var warehouseId = await GetDefaultWarehouseIdAsync(victim);
        (await RegisterStockIntakeAsync(victim, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();
        (await CreateSaleAsync(victim, SaleLine(productId, quantity: 1, unitPrice: 10m))).EnsureSuccessStatusCode();
        await CreateCustomerAsync(victim);
        await CreateSupplierAsync(victim);

        var attacker = await CreateBusinessAsync();

        // "<= 1" tolerance here would silently mask a real single-row leak on
        // any of these — a brand-new business must see exactly 0 rows on all
        // of them. /api/v1/users is excluded on purpose: it legitimately has
        // 1 row (the signup owner), checked separately below alongside the
        // warehouse special-case for the same reason.
        foreach (var path in new[]
                 {
                     "/api/v1/products", "/api/v1/sales", "/api/v1/customers", "/api/v1/suppliers",
                     "/api/v1/inventories", "/api/v1/batches", "/api/v1/purchases", "/api/v1/alerts",
                     "/api/v1/stock-movements", "/api/v1/reports"
                 })
        {
            var response = await attacker.GetAsync(path);
            response.EnsureSuccessStatusCode();

            var rows = (await ReadJsonAsync(response)).EnumerateArray().ToList();
            Assert.True(rows.Count == 0, $"{path} leaked {rows.Count} row(s) into a brand-new business");
        }

        // The attacker's own signup owner is the only row here, never the victim's team.
        var users = await attacker.GetAsync("/api/v1/users");
        users.EnsureSuccessStatusCode();
        Assert.Single((await ReadJsonAsync(users)).EnumerateArray());

        // The one warehouse a new business legitimately has is its own.
        var warehouses = await attacker.GetAsync("/api/v1/warehouses");
        warehouses.EnsureSuccessStatusCode();
        var ownWarehouses = (await ReadJsonAsync(warehouses)).EnumerateArray().ToList();
        Assert.Single(ownWarehouses);
        Assert.NotEqual(warehouseId, ownWarehouses[0].GetProperty("id").GetInt32());
    }

    private static async Task<int> CreateSoldSaleAsync(HttpClient client)
    {
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, SaleLine(productId, quantity: 2, unitPrice: 10m));
        sale.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CreateCustomerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Cliente de prueba",
            documentNumber = "12345678",
            phoneNumber = "999111222",
            email = "cliente@test.local",
            address = "Jr. Prueba 123"
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CreatePendingPurchaseOrderAsync(HttpClient client)
    {
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);

        var response = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10);
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
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

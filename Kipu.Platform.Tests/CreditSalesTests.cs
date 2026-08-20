using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X4 Bloque 1: a credit sale (Sale.Status == Credit, see
///     SalePaymentMethod.Credit) contributes nothing to revenue on its own —
///     only the installments actually collected against its PaymentPlan do,
///     on the day they were paid. See SalesContextFacade and the plan doc's
///     "Decisión confirmada" note on cancellation.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class CreditSalesTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private async Task<decimal> TotalSalesAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/dashboard/kpis");
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("totalSales").GetDecimal();
    }

    [Fact]
    public async Task CreditSale_ContributesNothingToRevenueUntilAnInstallmentIsPaid()
    {
        var client = await CreateBusinessAsync();
        // The sale's real line price is always the product's own BasePrice,
        // never the client-submitted SaleLine.unitPrice (see
        // SaleCommandService.Handle(CreateSaleCommand)) — set here so the
        // dollar amounts asserted below are the real ones.
        var productId = await CreateProductAsync(client, basePrice: 100m);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var before = await TotalSalesAsync(client);

        var sale = await CreateSaleAsync(client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();
        Assert.Equal(before, await TotalSalesAsync(client));

        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, totalInstallments = 2 });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();
        Assert.Equal(before, await TotalSalesAsync(client));

        (await client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();

        // Only the 50 actually collected shows up — not the sale's full 100.
        Assert.Equal(before + 50m, await TotalSalesAsync(client));

        (await client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();
        Assert.Equal(before + 100m, await TotalSalesAsync(client));
    }

    [Fact]
    public async Task PaymentPlan_CannotBeAttachedToAPaidSale()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, SaleLine(productId, quantity: 1, unitPrice: 50m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();

        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, totalInstallments = 2 });
        Assert.Equal(HttpStatusCode.Conflict, plan.StatusCode);
    }

    /// <summary>Rounding on a total that doesn't divide evenly must land exactly on the sale's total, remainder folded into the last installment — not silently drift the plan a cent short or over.</summary>
    [Fact]
    public async Task InstallmentAmounts_SumExactlyToTheSaleTotal_DespiteRounding()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client, basePrice: 100m);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        // 100 / 3 = 33.33 repeating — the classic rounding case.
        var sale = await CreateSaleAsync(client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();

        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, totalInstallments = 3 });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        for (var i = 0; i < 3; i++)
            (await client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();

        var final = await client.GetAsync($"/api/v1/payment-plans/by-sale/{saleId}");
        final.EnsureSuccessStatusCode();
        var payments = (await ReadJsonAsync(final)).GetProperty("payments").EnumerateArray()
            .Select(payment => payment.GetProperty("amount").GetDecimal())
            .ToList();

        Assert.Equal(3, payments.Count);
        Assert.Equal(100m, payments.Sum());
        Assert.Equal(33.33m, payments[0]);
        Assert.Equal(33.33m, payments[1]);
        Assert.Equal(33.34m, payments[2]);
    }

    [Fact]
    public async Task RevertingAPayment_RequiresAdmin_AndKeepsTheReversedRecordInTheTrail()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var cashier = await InviteAndSignInAsync(admin.Client, CashierRoleId);

        var productId = await CreateProductAsync(admin.Client, basePrice: 100m);
        var warehouseId = await GetDefaultWarehouseIdAsync(admin.Client);
        (await RegisterStockIntakeAsync(admin.Client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(admin.Client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();
        var plan = await admin.Client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, totalInstallments = 2 });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        (await admin.Client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();

        var deniedToCashier = await cashier.PostAsync($"/api/v1/payment-plans/{planId}/revert-last-payment", null);
        Assert.Equal(HttpStatusCode.Forbidden, deniedToCashier.StatusCode);

        var revenueBeforeRevert = await TotalSalesAsync(admin.Client);

        var reverted = await admin.Client.PostAsync($"/api/v1/payment-plans/{planId}/revert-last-payment", null);
        reverted.EnsureSuccessStatusCode();
        var plan2 = await ReadJsonAsync(reverted);
        Assert.Equal(0, plan2.GetProperty("paidInstallments").GetInt32());

        var payments = plan2.GetProperty("payments").EnumerateArray().ToList();
        Assert.Single(payments);
        Assert.True(payments[0].GetProperty("isReversed").GetBoolean());

        // The reversed payment no longer counts as revenue.
        Assert.Equal(revenueBeforeRevert - 50m, await TotalSalesAsync(admin.Client));
    }

    [Fact]
    public async Task RevertingAPayment_WithNoneLeft_Returns409()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var productId = await CreateProductAsync(admin.Client);
        var warehouseId = await GetDefaultWarehouseIdAsync(admin.Client);
        (await RegisterStockIntakeAsync(admin.Client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(admin.Client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();
        var plan = await admin.Client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, totalInstallments = 2 });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        var response = await admin.Client.PostAsync($"/api/v1/payment-plans/{planId}/revert-last-payment", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Decisión confirmada con el dueño (2026-08-20): cancelar una venta a crédito no devuelve lo ya cobrado.</summary>
    [Fact]
    public async Task CancellingACreditSale_KeepsAlreadyCollectedRevenue()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();
        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, totalInstallments = 2 });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        (await client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();
        var revenueAfterFirstInstallment = await TotalSalesAsync(client);

        (await client.PatchAsJsonAsync($"/api/v1/sales/{saleId}", new { status = "CANCELLED" })).EnsureSuccessStatusCode();

        // The plan is cancelled (no further installments can be taken) but the
        // 50 already collected stays counted — there is no refund flow.
        var byIdResponse = await client.GetAsync($"/api/v1/payment-plans/by-sale/{saleId}");
        byIdResponse.EnsureSuccessStatusCode();
        Assert.True((await ReadJsonAsync(byIdResponse)).GetProperty("isCancelled").GetBoolean());

        Assert.Equal(revenueAfterFirstInstallment, await TotalSalesAsync(client));
    }

    /// <summary>X4 M4: cancelling a sale reverses stock and revenue and is irreversible, so it's Admin only now — same override CustomersController's DELETE already has.</summary>
    [Fact]
    public async Task CancellingASale_RequiresAdmin()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var cashier = await InviteAndSignInAsync(admin.Client, CashierRoleId);

        var productId = await CreateProductAsync(admin.Client);
        var warehouseId = await GetDefaultWarehouseIdAsync(admin.Client);
        (await RegisterStockIntakeAsync(admin.Client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(admin.Client, SaleLine(productId, quantity: 1, unitPrice: 10m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();

        var deniedToCashier = await cashier.PatchAsJsonAsync($"/api/v1/sales/{saleId}", new { status = "CANCELLED" });
        Assert.Equal(HttpStatusCode.Forbidden, deniedToCashier.StatusCode);

        var cancelled = await admin.Client.PatchAsJsonAsync($"/api/v1/sales/{saleId}", new { status = "CANCELLED" });
        cancelled.EnsureSuccessStatusCode();
    }

    /// <summary>X4 M6: a deactivated customer stays in the table (see Customer.Deactivate) — a sale must not still be attributable to them.</summary>
    [Fact]
    public async Task SaleForADeactivatedCustomer_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var customerResponse = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = "Cliente de prueba",
            documentNumber = "12345678",
            phoneNumber = "999888777",
            email = "cliente@test.local"
        });
        customerResponse.EnsureSuccessStatusCode();
        var customerId = (await ReadJsonAsync(customerResponse)).GetProperty("id").GetInt32();

        (await client.DeleteAsync($"/api/v1/customers/{customerId}")).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/v1/sales", new
        {
            customerId,
            paymentMethod = "CASH",
            currency = "PEN",
            description = "venta de prueba",
            lines = new[] { SaleLine(productId, quantity: 1, unitPrice: 10m) }
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>X4 M5: an empty idempotency key is not "no key" — MySQL's unique index still enforces uniqueness on "" like any real value, so a second empty-keyed sale used to permanently 500.</summary>
    [Fact]
    public async Task TwoSalesWithAnEmptyIdempotencyKey_BothSucceed()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var payload = new
        {
            customerId = (int?)null,
            paymentMethod = "CASH",
            currency = "PEN",
            description = "venta de prueba",
            idempotencyKey = "",
            lines = new[] { SaleLine(productId, quantity: 1, unitPrice: 10m) }
        };

        var first = await client.PostAsJsonAsync("/api/v1/sales", payload);
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/sales", payload);
        second.EnsureSuccessStatusCode();

        // Two distinct sales, not the same one replayed — "" must behave like no key at all.
        Assert.NotEqual((await ReadJsonAsync(first)).GetProperty("id").GetInt32(),
            (await ReadJsonAsync(second)).GetProperty("id").GetInt32());
    }
}

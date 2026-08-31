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
    /// <summary>Two equal cuotas summing exactly to `total` — the schedule shape most of these tests just need, without caring about the exact dates.</summary>
    private static object[] TwoEqualInstallments(decimal total)
    {
        var half = total / 2;
        return
        [
            new { dueDate = "2026-09-15", amount = half },
            new { dueDate = "2026-10-15", amount = half }
        ];
    }

    private async Task<decimal> TotalSalesAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/dashboard/kpis");
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("totalSales").GetDecimal();
    }

    /// <summary>
    ///     X4: DashboardController's KPI is Admin-only, but a cashier's own POS
    ///     stats bar needs this same figure — SalesController exposes it
    ///     separately (Admin+Cashier) so the frontend never has to recompute
    ///     revenue itself (the root cause of A10: two places calculating the
    ///     same number, client-side one wrong).
    /// </summary>
    [Fact]
    public async Task SalesRevenueEndpoint_IsReachableByCashier_AndMatchesTheDashboardKpi()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var cashier = await InviteAndSignInAsync(admin.Client, CashierRoleId);

        var productId = await CreateProductAsync(admin.Client, basePrice: 100m);
        var warehouseId = await GetDefaultWarehouseIdAsync(admin.Client);
        (await RegisterStockIntakeAsync(admin.Client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(admin.Client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();
        var plan = await admin.Client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, schedule = TwoEqualInstallments(100m) });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();
        (await admin.Client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();

        var dashboardTotal = await TotalSalesAsync(admin.Client);

        var cashierResponse = await cashier.GetAsync("/api/v1/sales/revenue");
        cashierResponse.EnsureSuccessStatusCode();
        var cashierTotal = (await ReadJsonAsync(cashierResponse)).GetProperty("totalRevenue").GetDecimal();

        Assert.Equal(dashboardTotal, cashierTotal);
        Assert.Equal(50m, cashierTotal);
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

        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, schedule = TwoEqualInstallments(100m) });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();
        Assert.Equal(before, await TotalSalesAsync(client));

        (await client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();

        // Only the 50 actually collected shows up — not the sale's full 100.
        Assert.Equal(before + 50m, await TotalSalesAsync(client));

        (await client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();
        Assert.Equal(before + 100m, await TotalSalesAsync(client));
    }

    /// <summary>
    ///     X5 #5: a credit sale that's fully paid off must report that on
    ///     SaleResource itself — this is what lets the frontend show
    ///     "Completada" instead of "A crédito" forever. Covers both the
    ///     list endpoint (batched PaymentPlan lookup) and the single-sale
    ///     one, and confirms reverting the last payment flips it back.
    /// </summary>
    [Fact]
    public async Task IsFullyPaid_ReflectsThePlanAcrossListAndSingleEndpoints_AndFlipsBackOnRevert()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client, basePrice: 100m);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();

        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, schedule = TwoEqualInstallments(100m) });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        async Task<bool> IsFullyPaidInListAsync()
        {
            var sales = await ReadJsonAsync(await client.GetAsync("/api/v1/sales"));
            return sales.EnumerateArray().First(item => item.GetProperty("id").GetInt32() == saleId)
                .GetProperty("isFullyPaid").GetBoolean();
        }

        async Task<bool> IsFullyPaidByIdAsync()
        {
            var single = await ReadJsonAsync(await client.GetAsync($"/api/v1/sales/{saleId}"));
            return single.GetProperty("isFullyPaid").GetBoolean();
        }

        Assert.False(await IsFullyPaidInListAsync());
        Assert.False(await IsFullyPaidByIdAsync());

        (await client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();
        Assert.False(await IsFullyPaidInListAsync());

        (await client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();
        Assert.True(await IsFullyPaidInListAsync());
        Assert.True(await IsFullyPaidByIdAsync());

        (await client.PostAsync($"/api/v1/payment-plans/{planId}/revert-last-payment", null)).EnsureSuccessStatusCode();
        Assert.False(await IsFullyPaidInListAsync());
        Assert.False(await IsFullyPaidByIdAsync());
    }

    /// <summary>A cash sale never reports isFullyPaid, even though the field always defaults false anyway — asserted explicitly so a future change can't silently start reporting true for a Paid sale.</summary>
    [Fact]
    public async Task PaidSale_NeverReportsIsFullyPaid()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, SaleLine(productId, quantity: 1, unitPrice: 10m));
        sale.EnsureSuccessStatusCode();

        Assert.False((await ReadJsonAsync(sale)).GetProperty("isFullyPaid").GetBoolean());
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

        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, schedule = TwoEqualInstallments(50m) });
        Assert.Equal(HttpStatusCode.Conflict, plan.StatusCode);
    }

    /// <summary>
    ///     X6 #7: the cashier enters each cuota's date/amount by hand — the
    ///     schedule they submit (33.33/33.33/33.34, the classic
    ///     100/3-repeating remainder-on-the-last-cuota split) is exactly what
    ///     gets paid out, in DueDate order, no server-side recalculation.
    /// </summary>
    [Fact]
    public async Task InstallmentAmounts_ArePaidExactlyAsScheduled_InDueDateOrder()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client, basePrice: 100m);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();

        var schedule = new[]
        {
            new { dueDate = "2026-09-15", amount = 33.33m },
            new { dueDate = "2026-10-15", amount = 33.33m },
            new { dueDate = "2026-11-15", amount = 33.34m }
        };
        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, schedule });
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
        var plan = await admin.Client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, schedule = TwoEqualInstallments(100m) });
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
        var productId = await CreateProductAsync(admin.Client, basePrice: 100m);
        var warehouseId = await GetDefaultWarehouseIdAsync(admin.Client);
        (await RegisterStockIntakeAsync(admin.Client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(admin.Client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();
        var plan = await admin.Client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, schedule = TwoEqualInstallments(100m) });
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
        var productId = await CreateProductAsync(client, basePrice: 100m);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, "CREDIT", SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();
        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, schedule = TwoEqualInstallments(100m) });
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

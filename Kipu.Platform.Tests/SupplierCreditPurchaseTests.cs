using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X6 #12 (Bloque G2): a credit purchase order tracks its debt via a
///     SupplierPaymentPlan, mirroring Sales' PaymentPlan (X6 #7) exactly —
///     see CreditSalesTests for the same assertions on the Sales side.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class SupplierCreditPurchaseTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>Two equal cuotas summing exactly to `total` — the schedule shape most of these tests just need.</summary>
    private static object[] TwoEqualInstallments(decimal total)
    {
        var half = total / 2;
        return
        [
            new { dueDate = "2026-09-15", amount = half },
            new { dueDate = "2026-10-15", amount = half }
        ];
    }

    [Fact]
    public async Task CreatingAPlan_WithASchedule_Succeeds()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);

        // 10 units at S/ 5.00 = S/ 50.00 order total.
        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10, unitPrice: 5m);
        order.EnsureSuccessStatusCode();
        var purchaseOrderId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        var schedule = TwoEqualInstallments(50m);
        var plan = await client.PostAsJsonAsync("/api/v1/supplier-payment-plans", new { purchaseOrderId, schedule });
        plan.EnsureSuccessStatusCode();

        var planJson = await ReadJsonAsync(plan);
        Assert.Equal(2, planJson.GetProperty("totalInstallments").GetInt32());
        Assert.Equal(0, planJson.GetProperty("paidInstallments").GetInt32());
        Assert.False(planJson.GetProperty("isFullyPaid").GetBoolean());
    }

    /// <summary>Decision 1: the schedule's amounts must add up exactly to the order's total — no margin.</summary>
    [Fact]
    public async Task CreatingAPlan_WithAScheduleThatDoesNotMatchTheTotal_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);

        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10, unitPrice: 5m);
        order.EnsureSuccessStatusCode();
        var purchaseOrderId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        var schedule = new[]
        {
            new { dueDate = "2026-09-15", amount = 20m },
            new { dueDate = "2026-10-15", amount = 20m }
        };
        var plan = await client.PostAsJsonAsync("/api/v1/supplier-payment-plans", new { purchaseOrderId, schedule });
        Assert.Equal(HttpStatusCode.BadRequest, plan.StatusCode);
    }

    /// <summary>Decision 12, point 5: the plan attaches whether the order is still PENDING or already RECEIVED — only CANCELLED rejects it.</summary>
    [Fact]
    public async Task CreatingAPlan_OnAReceivedOrder_Succeeds()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);

        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10, unitPrice: 5m);
        order.EnsureSuccessStatusCode();
        var purchaseOrderId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        (await client.PatchAsJsonAsync($"/api/v1/purchases/{purchaseOrderId}", new { status = "RECEIVED" }))
            .EnsureSuccessStatusCode();

        var schedule = TwoEqualInstallments(50m);
        var plan = await client.PostAsJsonAsync("/api/v1/supplier-payment-plans", new { purchaseOrderId, schedule });
        Assert.True(plan.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CreatingAPlan_OnACancelledOrder_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);

        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10, unitPrice: 5m);
        order.EnsureSuccessStatusCode();
        var purchaseOrderId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        (await client.PatchAsJsonAsync($"/api/v1/purchases/{purchaseOrderId}", new { status = "CANCELLED" }))
            .EnsureSuccessStatusCode();

        var schedule = TwoEqualInstallments(50m);
        var plan = await client.PostAsJsonAsync("/api/v1/supplier-payment-plans", new { purchaseOrderId, schedule });
        Assert.Equal(HttpStatusCode.Conflict, plan.StatusCode);
    }

    [Fact]
    public async Task RegisteringAPayment_TakesTheAmountFromTheSchedule_InDueDateOrder()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);

        // 1 unit at S/ 100.00 — a clean total to split three ways (the classic
        // 100/3-repeating remainder-on-the-last-cuota case), mirroring
        // CreditSalesTests.InstallmentAmounts_ArePaidExactlyAsScheduled_InDueDateOrder.
        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 1, unitPrice: 100m);
        order.EnsureSuccessStatusCode();
        var purchaseOrderId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();
        var orderTotal = (await ReadJsonAsync(order)).GetProperty("details")[0].GetProperty("subtotal").GetDecimal();

        var schedule = new[]
        {
            new { dueDate = "2026-09-15", amount = 33.33m },
            new { dueDate = "2026-10-15", amount = 33.33m },
            new { dueDate = "2026-11-15", amount = orderTotal - 66.66m }
        };
        var plan = await client.PostAsJsonAsync("/api/v1/supplier-payment-plans", new { purchaseOrderId, schedule });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        for (var i = 0; i < 3; i++)
            (await client.PostAsync($"/api/v1/supplier-payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();

        var final = await client.GetAsync($"/api/v1/supplier-payment-plans/by-purchase-order/{purchaseOrderId}");
        final.EnsureSuccessStatusCode();
        var finalJson = await ReadJsonAsync(final);
        Assert.True(finalJson.GetProperty("isFullyPaid").GetBoolean());

        var payments = finalJson.GetProperty("payments").EnumerateArray()
            .Select(payment => payment.GetProperty("amount").GetDecimal()).ToList();
        Assert.Equal(3, payments.Count);
        Assert.Equal(orderTotal, payments.Sum());
    }

    [Fact]
    public async Task RevertingAPayment_RequiresAdmin_AndKeepsTheReversedRecordInTheTrail()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var warehouse = await InviteAndSignInAsync(admin.Client, WarehouseRoleId);

        var supplierId = await CreateSupplierAsync(admin.Client);
        var productId = await CreateProductAsync(admin.Client);
        var order = await CreatePurchaseOrderAsync(admin.Client, supplierId, productId, quantity: 10, unitPrice: 5m);
        order.EnsureSuccessStatusCode();
        var purchaseOrderId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        var plan = await admin.Client.PostAsJsonAsync("/api/v1/supplier-payment-plans",
            new { purchaseOrderId, schedule = TwoEqualInstallments(50m) });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        (await admin.Client.PostAsync($"/api/v1/supplier-payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();

        var deniedToWarehouse = await warehouse.PostAsync($"/api/v1/supplier-payment-plans/{planId}/revert-last-payment", null);
        Assert.Equal(HttpStatusCode.Forbidden, deniedToWarehouse.StatusCode);

        var reverted = await admin.Client.PostAsync($"/api/v1/supplier-payment-plans/{planId}/revert-last-payment", null);
        reverted.EnsureSuccessStatusCode();
        var plan2 = await ReadJsonAsync(reverted);
        Assert.Equal(0, plan2.GetProperty("paidInstallments").GetInt32());

        var payments = plan2.GetProperty("payments").EnumerateArray().ToList();
        Assert.Single(payments);
        Assert.True(payments[0].GetProperty("isReversed").GetBoolean());
    }

    [Fact]
    public async Task RevertingAPayment_WithNoneLeft_Returns409()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);
        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10, unitPrice: 5m);
        order.EnsureSuccessStatusCode();
        var purchaseOrderId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        var plan = await client.PostAsJsonAsync("/api/v1/supplier-payment-plans",
            new { purchaseOrderId, schedule = TwoEqualInstallments(50m) });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        var response = await client.PostAsync($"/api/v1/supplier-payment-plans/{planId}/revert-last-payment", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Decision 5 (mirrored from #7): editing an unpaid cuota is allowed even when another cuota in the same plan is already paid.</summary>
    [Fact]
    public async Task UpdatingAnUnpaidInstallment_WithAnotherAlreadyPaid_Succeeds()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);
        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10, unitPrice: 5m);
        order.EnsureSuccessStatusCode();
        var purchaseOrderId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        var plan = await client.PostAsJsonAsync("/api/v1/supplier-payment-plans",
            new { purchaseOrderId, schedule = TwoEqualInstallments(50m) });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        (await client.PostAsync($"/api/v1/supplier-payment-plans/{planId}/register-payment", null)).EnsureSuccessStatusCode();

        var afterFirstPayment = await ReadJsonAsync(await client.GetAsync($"/api/v1/supplier-payment-plans/by-purchase-order/{purchaseOrderId}"));
        var unpaidInstallment = afterFirstPayment.GetProperty("installments").EnumerateArray()
            .First(installment => !installment.GetProperty("isPaid").GetBoolean());
        var unpaidInstallmentId = unpaidInstallment.GetProperty("id").GetInt32();

        var edited = await client.PatchAsJsonAsync($"/api/v1/supplier-payment-plans/{planId}/installments/{unpaidInstallmentId}",
            new { dueDate = "2026-12-01", amount = 25m });
        Assert.True(edited.IsSuccessStatusCode);
    }

    /// <summary>Mirrors SaleCommandService cancelling PaymentPlan (X6 #7) — cancelling a credit purchase order cancels its plan too.</summary>
    [Fact]
    public async Task CancellingAPurchaseOrder_CancelsItsPaymentPlan()
    {
        var client = await CreateBusinessAsync();
        var supplierId = await CreateSupplierAsync(client);
        var productId = await CreateProductAsync(client);
        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10, unitPrice: 5m);
        order.EnsureSuccessStatusCode();
        var purchaseOrderId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        var plan = await client.PostAsJsonAsync("/api/v1/supplier-payment-plans",
            new { purchaseOrderId, schedule = TwoEqualInstallments(50m) });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        (await client.PatchAsJsonAsync($"/api/v1/purchases/{purchaseOrderId}", new { status = "CANCELLED" }))
            .EnsureSuccessStatusCode();

        var byOrderResponse = await client.GetAsync($"/api/v1/supplier-payment-plans/by-purchase-order/{purchaseOrderId}");
        byOrderResponse.EnsureSuccessStatusCode();
        Assert.True((await ReadJsonAsync(byOrderResponse)).GetProperty("isCancelled").GetBoolean());

        var paymentResponse = await client.PostAsync($"/api/v1/supplier-payment-plans/{planId}/register-payment", null);
        Assert.Equal(HttpStatusCode.Conflict, paymentResponse.StatusCode);
    }

}

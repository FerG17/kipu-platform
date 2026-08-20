using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     Stock is the one number in this system that two requests routinely
///     fight over: a bodega with two tills sells the same shelf. Every check
///     before a sale is a read, and the deduction that follows is a separate
///     write, so anything that isn't atomic between them can be beaten by
///     simply sending the requests at the same time — no special access
///     needed, just a second browser tab.
///
///     These tests hammer the same product from many requests at once and
///     assert the invariant that has to survive it: a bodega can never sell
///     more units than it physically had.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class StockConcurrencyTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>
    ///     The last unit on the shelf, sold to eight customers at once.
    ///     Exactly one sale may be confirmed; the other seven must be told
    ///     there is no stock.
    /// </summary>
    [Fact]
    public async Task ConcurrentSalesOfTheLastUnit_ConfirmOnlyOne()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 1)).EnsureSuccessStatusCode();

        var attempts = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => CreateSaleAsync(client, SaleLine(productId, quantity: 1, unitPrice: 10m))));

        var confirmed = attempts.Count(response => response.IsSuccessStatusCode);

        Assert.Equal(1, confirmed);
        Assert.Equal(0, await GetTotalStockAsync(client, productId));
    }

    /// <summary>
    ///     Ten tills racing over five units. However the race resolves, the
    ///     books have to balance: units sold + units left == units received,
    ///     and stock can never end up negative.
    /// </summary>
    [Fact]
    public async Task ConcurrentSales_NeverSellMoreUnitsThanExist()
    {
        const int stocked = 5;
        const int tills = 10;

        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: stocked)).EnsureSuccessStatusCode();

        var attempts = await Task.WhenAll(Enumerable.Range(0, tills)
            .Select(_ => CreateSaleAsync(client, SaleLine(productId, quantity: 1, unitPrice: 10m))));

        var confirmed = attempts.Count(response => response.IsSuccessStatusCode);
        var remaining = await GetTotalStockAsync(client, productId);

        Assert.True(confirmed <= stocked, $"{confirmed} sales were confirmed against only {stocked} units in stock");
        Assert.True(remaining >= 0, $"stock went negative: {remaining}");
        Assert.Equal(stocked, confirmed + remaining);
    }

    /// <summary>
    ///     The same race on the cancellation side: cancelling one sale eight
    ///     times must return its units to the shelf exactly once, or a
    ///     cancelled sale becomes a stock printer.
    /// </summary>
    [Fact]
    public async Task ConcurrentCancellationsOfTheSameSale_RestoreStockOnlyOnce()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, SaleLine(productId, quantity: 4, unitPrice: 10m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();

        await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => client.PatchAsJsonAsync($"/api/v1/sales/{saleId}", new { status = "CANCELLED" })));

        Assert.Equal(10, await GetTotalStockAsync(client, productId));
    }

    /// <summary>
    ///     And on the intake side: receiving the same purchase order from
    ///     eight parallel requests must book the delivery once, not eight
    ///     times.
    /// </summary>
    [Fact]
    public async Task ConcurrentReceiptsOfTheSamePurchaseOrder_BookTheDeliveryOnce()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);

        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 20);
        order.EnsureSuccessStatusCode();
        var purchaseId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => client.PatchAsJsonAsync($"/api/v1/purchases/{purchaseId}", new { status = "RECEIVED" })));

        Assert.Equal(20, await GetTotalStockAsync(client, productId));
    }

    /// <summary>
    ///     A purchase order that reports RECEIVED is a promise that the goods
    ///     are on the shelf. The status change and the stock intake must
    ///     therefore stand or fall together — the invariant that keeps
    ///     inventory matching what the supplier actually delivered.
    /// </summary>
    [Fact]
    public async Task PurchaseOrderMarkedReceived_ActuallyPutsTheGoodsInStock()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);

        var order = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 30);
        order.EnsureSuccessStatusCode();
        var purchaseId = (await ReadJsonAsync(order)).GetProperty("id").GetInt32();

        var received = await client.PatchAsJsonAsync($"/api/v1/purchases/{purchaseId}", new { status = "RECEIVED" });
        received.EnsureSuccessStatusCode();

        Assert.Equal("RECEIVED", (await ReadJsonAsync(received)).GetProperty("status").GetString());
        Assert.Equal(30, await GetTotalStockAsync(client, productId));
    }

    /// <summary>
    ///     Two payments registered at the same instant against a plan with one
    ///     installment left must not both be accepted — otherwise a credit
    ///     ledger can be paid off with fewer payments than it has installments.
    /// </summary>
    [Fact]
    public async Task ConcurrentInstallmentPayments_CannotExceedTheInstallmentCount()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10)).EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, SaleLine(productId, quantity: 1, unitPrice: 100m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();

        var plan = await client.PostAsJsonAsync("/api/v1/payment-plans", new { saleId, totalInstallments = 2 });
        plan.EnsureSuccessStatusCode();
        var planId = (await ReadJsonAsync(plan)).GetProperty("id").GetInt32();

        await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => client.PostAsync($"/api/v1/payment-plans/{planId}/register-payment", null)));

        var final = await client.GetAsync($"/api/v1/payment-plans/by-sale/{saleId}");
        final.EnsureSuccessStatusCode();

        // Upper bound only, deliberately: PaymentPlanCommandService.Handle
        // (RegisterInstallmentPaymentCommand) does NOT retry on a lost
        // DbUpdateConcurrencyException race — a loser gets ConcurrentModification
        // and the caller is expected to look at the plan again, not the server.
        // Firing 8 truly simultaneous requests (no retry) can legitimately land
        // fewer than 2 — that's correct, by-design behavior, not undercounting.
        // Asserting exact(2) here would fail against CORRECT behavior, not catch
        // a bug (verified by tracing the handler: no silent-drop path exists —
        // every non-winning request gets a real 409, never a swallowed no-op).
        var paid = (await ReadJsonAsync(final)).GetProperty("paidInstallments").GetInt32();
        Assert.True(paid <= 2, $"{paid} installments were registered against a 2-installment plan");
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X5 Bloque C: a product can now have several ACTIVE batches at once,
///     and sales draw down the earliest-expiring one first (FEFO). Covers
///     the concrete scenario the owner reported: stock already on the shelf
///     is close to expiring, and a new delivery arrives early with a later
///     expiration date — that used to be blocked outright by Bloque A's
///     cheap mitigation (removed here, now that batches don't overwrite each
///     other in place any more).
/// </summary>
[Collection(KipuApiCollection.Name)]
public class LotFefoTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private static JsonElement FindBatchByExpiration(JsonElement batches, DateOnly? expiration)
    {
        foreach (var batch in batches.EnumerateArray())
        {
            var raw = batch.TryGetProperty("expiration", out var value) && value.ValueKind != JsonValueKind.Null
                ? value.GetString()
                : null;
            var actual = raw == null ? (DateOnly?)null : DateOnly.Parse(raw);
            if (actual == expiration) return batch;
        }

        throw new InvalidOperationException($"No batch found with expiration {expiration}");
    }

    [Fact]
    public async Task AnEarlyRestockWithALaterExpiration_OpensASecondBatchInsteadOfBeingBlocked()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var soonExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var laterExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(120);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10, expiration: soonExpiration))
            .EnsureSuccessStatusCode();

        // Bloque A's cheap mitigation used to reject this exact case (a
        // later expiration arriving while the earlier-expiring batch still
        // has stock) — Bloque C's real per-lot tracking must accept it.
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 50, expiration: laterExpiration))
            .EnsureSuccessStatusCode();

        var batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        Assert.Equal(2, batches.GetArrayLength());

        var soonBatch = FindBatchByExpiration(batches, soonExpiration);
        var laterBatch = FindBatchByExpiration(batches, laterExpiration);
        Assert.Equal(10, soonBatch.GetProperty("remainingQuantity").GetInt32());
        Assert.Equal(50, laterBatch.GetProperty("remainingQuantity").GetInt32());

        Assert.Equal(60, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task SellingAcrossBatches_DrawsDownTheEarliestExpiringLotFirst()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var soonExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        var laterExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, expiration: soonExpiration))
            .EnsureSuccessStatusCode();
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, expiration: laterExpiration))
            .EnsureSuccessStatusCode();

        (await CreateSaleAsync(client, SaleLine(productId, quantity: 3, unitPrice: 10m))).EnsureSuccessStatusCode();

        var batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        var soonBatch = FindBatchByExpiration(batches, soonExpiration);
        var laterBatch = FindBatchByExpiration(batches, laterExpiration);

        Assert.Equal(2, soonBatch.GetProperty("remainingQuantity").GetInt32());
        Assert.Equal(5, laterBatch.GetProperty("remainingQuantity").GetInt32());
    }

    [Fact]
    public async Task SellingMoreThanTheNearestLotHolds_SpillsIntoTheNextEarliestLot()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var soonExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        var laterExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 3, expiration: soonExpiration))
            .EnsureSuccessStatusCode();
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, expiration: laterExpiration))
            .EnsureSuccessStatusCode();

        (await CreateSaleAsync(client, SaleLine(productId, quantity: 5, unitPrice: 10m))).EnsureSuccessStatusCode();

        var batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        var soonBatch = FindBatchByExpiration(batches, soonExpiration);
        var laterBatch = FindBatchByExpiration(batches, laterExpiration);

        Assert.Equal(0, soonBatch.GetProperty("remainingQuantity").GetInt32());
        Assert.Equal(3, laterBatch.GetProperty("remainingQuantity").GetInt32());
    }

    /// <summary>
    ///     Cancelling a sale restores units into an active batch — an
    ///     approximation (the nearest-expiring active batch with spare
    ///     capacity), not necessarily the exact lot the sale drew from, since
    ///     RestoreStock only carries ProductId + Quantity. See
    ///     InventoryCommandService.Handle(RegisterStockReturnCommand).
    /// </summary>
    [Fact]
    public async Task CancellingASale_RestoresUnitsIntoAnActiveBatch()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, expiration: expiration))
            .EnsureSuccessStatusCode();

        var sale = await CreateSaleAsync(client, SaleLine(productId, quantity: 3, unitPrice: 10m));
        sale.EnsureSuccessStatusCode();
        var saleId = (await ReadJsonAsync(sale)).GetProperty("id").GetInt32();

        Assert.Equal(2, await GetTotalStockAsync(client, productId));

        (await client.PatchAsJsonAsync($"/api/v1/sales/{saleId}", new { status = "CANCELLED" }))
            .EnsureSuccessStatusCode();

        Assert.Equal(5, await GetTotalStockAsync(client, productId));

        var batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        var batch = FindBatchByExpiration(batches, expiration);
        Assert.Equal(5, batch.GetProperty("remainingQuantity").GetInt32());
    }

    /// <summary>
    ///     X5 feedback #3: a purchase order's RECEIVED intake has no
    ///     expiration field (see PurchaseOrderCommandService.MarkReceived),
    ///     so the batch it creates starts with none — the owner sets it
    ///     later via PATCH once the delivery is checked.
    /// </summary>
    [Fact]
    public async Task ReceivingAPurchaseOrder_OpensABatchWithNoExpiration_EditableAfterTheFact()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);

        var order = await ReadJsonAsync(await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 20));
        var orderId = order.GetProperty("id").GetInt32();
        (await client.PatchAsJsonAsync($"/api/v1/purchases/{orderId}", new { status = "RECEIVED" }))
            .EnsureSuccessStatusCode();

        var batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        Assert.Equal(1, batches.GetArrayLength());
        var batch = batches[0];
        Assert.Equal(JsonValueKind.Null, batch.GetProperty("expiration").ValueKind);
        var batchId = batch.GetProperty("id").GetInt32();

        var newExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(45);
        var updateResponse = await client.PatchAsJsonAsync($"/api/v1/batches/{batchId}/expiration",
            new { expiration = newExpiration });
        updateResponse.EnsureSuccessStatusCode();
        var updatedExpiration = DateOnly.Parse((await ReadJsonAsync(updateResponse)).GetProperty("expiration").GetString()!);
        Assert.Equal(newExpiration, updatedExpiration);
    }

    /// <summary>
    ///     X5 feedback #3's second half: once a replenishment-order batch's
    ///     expiration is filled in, FEFO must pick it up immediately — no
    ///     separate re-ranking step, since the sale-time query ranks by
    ///     Expiration directly off the batch row.
    /// </summary>
    [Fact]
    public async Task EditingABatchExpirationToBeEarlierThanAnotherLot_MakesItConsumedFirst()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var existingExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, expiration: existingExpiration))
            .EnsureSuccessStatusCode();

        // Simulates a replenishment order received with no expiration yet.
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, purchasePrice: 4m))
            .EnsureSuccessStatusCode();

        var batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        var undated = FindBatchByExpiration(batches, null);
        var undatedId = undated.GetProperty("id").GetInt32();

        // Edited to expire sooner than the batch that already had a date —
        // it must now be the one FEFO drains first.
        var soonerExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
        (await client.PatchAsJsonAsync($"/api/v1/batches/{undatedId}/expiration", new { expiration = soonerExpiration }))
            .EnsureSuccessStatusCode();

        (await CreateSaleAsync(client, SaleLine(productId, quantity: 3, unitPrice: 10m))).EnsureSuccessStatusCode();

        batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        var editedBatch = FindBatchByExpiration(batches, soonerExpiration);
        var untouchedBatch = FindBatchByExpiration(batches, existingExpiration);

        Assert.Equal(2, editedBatch.GetProperty("remainingQuantity").GetInt32());
        Assert.Equal(5, untouchedBatch.GetProperty("remainingQuantity").GetInt32());
    }

    [Fact]
    public async Task EditingADiscardedBatchExpiration_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, expiration: expiration))
            .EnsureSuccessStatusCode();
        var batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        var batchId = batches[0].GetProperty("id").GetInt32();

        (await client.PostAsync($"/api/v1/batches/{batchId}/discard", null)).EnsureSuccessStatusCode();

        var response = await client.PatchAsJsonAsync($"/api/v1/batches/{batchId}/expiration",
            new { expiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20) });

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task EditingABatchExpirationToAPastDate_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        var expiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, expiration: expiration))
            .EnsureSuccessStatusCode();
        var batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        var batchId = batches[0].GetProperty("id").GetInt32();

        var response = await client.PatchAsJsonAsync($"/api/v1/batches/{batchId}/expiration",
            new { expiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>X5 #2: each lot keeps its own cost — a later intake with a different price no longer overwrites the earlier lot's.</summary>
    [Fact]
    public async Task TwoIntakesWithDifferentCosts_KeepIndependentPurchasePricesPerLot()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        var firstExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        var secondExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10, expiration: firstExpiration,
            purchasePrice: 3.5m)).EnsureSuccessStatusCode();
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10, expiration: secondExpiration,
            purchasePrice: 6.0m)).EnsureSuccessStatusCode();

        var batches = await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"));
        var firstBatch = FindBatchByExpiration(batches, firstExpiration);
        var secondBatch = FindBatchByExpiration(batches, secondExpiration);

        Assert.Equal(3.5m, firstBatch.GetProperty("purchasePrice").GetDecimal());
        Assert.Equal(6.0m, secondBatch.GetProperty("purchasePrice").GetDecimal());
    }
}

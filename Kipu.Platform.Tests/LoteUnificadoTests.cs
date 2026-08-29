using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X6 "Bloque Lote unificado" (#3+#10+#11): a free-text lot name
///     (Batch.Label) threaded through intake/PO-receiving/manual editing, and
///     a manual stock adjustment now always lands in a specific batch instead
///     of only ever touching the aggregate InventoryItem total — removal via
///     automatic FEFO (same draw-down a sale uses), addition either into an
///     existing lot the owner picked or a freshly-opened one.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class LoteUnificadoTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private static JsonElement SingleActiveBatch(JsonElement batches)
    {
        return Assert.Single(batches.EnumerateArray(), batch => batch.GetProperty("status").GetString() == "ACTIVE");
    }

    [Fact]
    public async Task RegisteringAStockIntake_WithOnlyALabel_StillOpensABatchCarryingIt()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10, label: "Lote enero"))
            .EnsureSuccessStatusCode();

        var batch = SingleActiveBatch(await GetBatchesAsync(client, productId));
        Assert.Equal("Lote enero", batch.GetProperty("label").GetString());
    }

    [Fact]
    public async Task ReceivingAPurchaseOrder_WithABatchLabelOnTheLine_CarriesItOntoTheOpenedBatch()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);

        var orderResponse = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 20, unitPrice: 4m,
            batchLabel: "Lote proveedor ABC");
        orderResponse.EnsureSuccessStatusCode();
        var orderId = (await ReadJsonAsync(orderResponse)).GetProperty("id").GetInt32();

        (await client.PatchAsJsonAsync($"/api/v1/purchases/{orderId}", new { status = "RECEIVED" })).EnsureSuccessStatusCode();

        var batch = SingleActiveBatch(await GetBatchesAsync(client, productId));
        Assert.Equal("Lote proveedor ABC", batch.GetProperty("label").GetString());
    }

    [Fact]
    public async Task EditingABatch_UpdatesItsLabelAlongsideExpiration()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, purchasePrice: 2m)).EnsureSuccessStatusCode();
        var batchId = SingleActiveBatch(await GetBatchesAsync(client, productId)).GetProperty("id").GetInt32();

        var newExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var response = await client.PatchAsJsonAsync($"/api/v1/batches/{batchId}/expiration",
            new { expiration = newExpiration, label = "Lote renombrado" });
        response.EnsureSuccessStatusCode();

        var body = await ReadJsonEnvelopeAsync(response);
        Assert.Equal("Lote renombrado", body.GetProperty("label").GetString());
        Assert.Equal(newExpiration, DateOnly.Parse(body.GetProperty("expiration").GetString()!));
    }

    [Fact]
    public async Task AdjustStock_RemovingUnits_DrawsDownTheEarliestExpiringBatchFirst()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        var soonExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
        var laterExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);

        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10, expiration: soonExpiration))
            .EnsureSuccessStatusCode();
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10, expiration: laterExpiration))
            .EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment",
            new { warehouseId, delta = -7, reason = "Merma: conteo físico" });
        response.EnsureSuccessStatusCode();

        var batches = (await GetBatchesAsync(client, productId)).EnumerateArray().ToList();
        var soonBatch = batches.Single(b => DateOnly.Parse(b.GetProperty("expiration").GetString()!) == soonExpiration);
        var laterBatch = batches.Single(b => DateOnly.Parse(b.GetProperty("expiration").GetString()!) == laterExpiration);

        Assert.Equal(3, soonBatch.GetProperty("remainingQuantity").GetDecimal());
        Assert.Equal(10, laterBatch.GetProperty("remainingQuantity").GetDecimal());
        Assert.Equal(13, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task AdjustStock_ThatFullyDepletesABatch_DiscardsIt()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5,
            expiration: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10))).EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment",
            new { warehouseId, delta = -5, reason = "Merma total" })).EnsureSuccessStatusCode();

        var batches = (await ReadJsonAsync(await client.GetAsync($"/api/v1/batches?productId={productId}"))).EnumerateArray();
        var batch = Assert.Single(batches);
        Assert.Equal("INACTIVE", batch.GetProperty("status").GetString());
        Assert.Equal(0, batch.GetProperty("remainingQuantity").GetDecimal());
    }

    [Fact]
    public async Task AdjustStock_AddingToAnExistingBatch_IncreasesItsQuantityAndRemainingQuantity()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 10, purchasePrice: 2m)).EnsureSuccessStatusCode();
        var batchId = SingleActiveBatch(await GetBatchesAsync(client, productId)).GetProperty("id").GetInt32();

        var response = await client.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment",
            new { warehouseId, delta = 6, reason = "Se encontraron más unidades del mismo lote", batchId });
        response.EnsureSuccessStatusCode();

        var batch = SingleActiveBatch(await GetBatchesAsync(client, productId));
        Assert.Equal(16, batch.GetProperty("quantity").GetDecimal());
        Assert.Equal(16, batch.GetProperty("remainingQuantity").GetDecimal());
        Assert.Equal(16, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task AdjustStock_AddingToABatchOfADifferentProduct_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        var productId = await CreateProductAsync(client, name: "Producto A");
        var otherProductId = await CreateProductAsync(client, name: "Producto B");
        (await RegisterStockIntakeAsync(client, otherProductId, warehouseId, quantity: 10, purchasePrice: 2m)).EnsureSuccessStatusCode();
        var otherBatchId = SingleActiveBatch(await GetBatchesAsync(client, otherProductId)).GetProperty("id").GetInt32();
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment",
            new { warehouseId, delta = 3, reason = "intento cruzado", batchId = otherBatchId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(5, await GetTotalStockAsync(client, productId));
    }

    [Fact]
    public async Task AdjustStock_AddingToADiscardedBatch_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5, purchasePrice: 2m)).EnsureSuccessStatusCode();
        var batchId = SingleActiveBatch(await GetBatchesAsync(client, productId)).GetProperty("id").GetInt32();
        (await client.PostAsync($"/api/v1/batches/{batchId}/discard", null)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment",
            new { warehouseId, delta = 3, reason = "no debería aplicar", batchId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AdjustStock_AddingWithoutABatchId_OpensANewLabeledBatch()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        var newExpiration = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15);
        // An adjustment corrects an existing InventoryItem's count — it
        // never creates the item itself, same as every other adjust test.
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 0)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/api/v1/inventories/{productId}/adjustment", new
        {
            warehouseId, delta = 8, reason = "Conteo físico: se halló stock sin registrar",
            newBatchExpiration = newExpiration, newBatchPurchasePrice = 3.5m, newBatchLabel = "Lote hallado"
        });
        response.EnsureSuccessStatusCode();

        var batch = SingleActiveBatch(await GetBatchesAsync(client, productId));
        Assert.Equal("Lote hallado", batch.GetProperty("label").GetString());
        Assert.Equal(newExpiration, DateOnly.Parse(batch.GetProperty("expiration").GetString()!));
        Assert.Equal(3.5m, batch.GetProperty("purchasePrice").GetDecimal());
        Assert.Equal(8, batch.GetProperty("quantity").GetDecimal());
    }
}

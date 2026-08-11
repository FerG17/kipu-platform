using System.Net.Http.Json;
using Bodega.Platform.Tests.Infrastructure;

namespace Bodega.Platform.Tests;

/// <summary>
///     The entradas/salidas report and its three combinable filters.
/// </summary>
[Collection(BodegaApiCollection.Name)]
public class ReportFilterTests(BodegaApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>
    ///     The supplier filter used to match on the supplier's *name* against
    ///     the free-text field copied onto each stock movement. Renaming a
    ///     supplier therefore orphaned every movement it had already produced:
    ///     the report silently came back empty instead of showing its history.
    /// </summary>
    [Fact]
    public async Task StockMovementReport_FilteredBySupplier_StillFindsMovementsAfterTheSupplierIsRenamed()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client, "Distribuidora", "Norte");
        var purchaseOrderId = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 15);

        (await client.PatchAsJsonAsync($"/api/v1/purchases/{purchaseOrderId}", new { status = "RECEIVED" }))
            .EnsureSuccessStatusCode();

        // The shop corrects the supplier's name afterwards — a perfectly
        // ordinary edit that must not erase its purchase history.
        (await client.PatchAsJsonAsync($"/api/v1/suppliers/{supplierId}", new
        {
            name = "Distribuidora Norte S.A.C.",
            lastName = "",
            ruc = "20123456789",
            email = "proveedor@test.local",
            phone = "999999999",
            address = "Av. Siempre Viva",
            contactPerson = "Contacto",
            category = "ABARROTES"
        })).EnsureSuccessStatusCode();

        var csv = await GenerateAndExportStockMovementReportAsync(client, supplierId);

        Assert.Contains("INTAKE", csv);
        Assert.Contains("15", csv);
    }

    /// <summary>The filter still has to exclude other suppliers' movements.</summary>
    [Fact]
    public async Task StockMovementReport_FilteredBySupplier_ExcludesAnotherSuppliersMovements()
    {
        var client = await CreateBusinessAsync();
        var wantedProductId = await CreateProductAsync(client);
        var otherProductId = await CreateProductAsync(client);

        var wantedSupplierId = await CreateSupplierAsync(client, "Buscado", "Proveedor");
        var otherSupplierId = await CreateSupplierAsync(client, "Otro", "Proveedor");

        var wantedOrder = await CreatePurchaseOrderAsync(client, wantedSupplierId, wantedProductId, quantity: 7);
        var otherOrder = await CreatePurchaseOrderAsync(client, otherSupplierId, otherProductId, quantity: 99);

        (await client.PatchAsJsonAsync($"/api/v1/purchases/{wantedOrder}", new { status = "RECEIVED" })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/v1/purchases/{otherOrder}", new { status = "RECEIVED" })).EnsureSuccessStatusCode();

        var csv = await GenerateAndExportStockMovementReportAsync(client, wantedSupplierId);

        Assert.Contains(",7,", csv);
        Assert.DoesNotContain(",99,", csv);
    }

    private static async Task<string> GenerateAndExportStockMovementReportAsync(HttpClient client, int supplierId)
    {
        var reportResponse = await client.PostAsJsonAsync("/api/v1/reports", new
        {
            type = "STOCK_MOVEMENTS",
            dateFrom = (DateOnly?)null,
            dateTo = (DateOnly?)null,
            productId = (int?)null,
            supplierId
        });
        reportResponse.EnsureSuccessStatusCode();
        var reportId = (await ReadJsonAsync(reportResponse)).GetProperty("id").GetInt32();

        var exportResponse = await client.GetAsync($"/api/v1/reports/{reportId}/export");
        exportResponse.EnsureSuccessStatusCode();
        return await exportResponse.Content.ReadAsStringAsync();
    }

    private static async Task<int> CreateSupplierAsync(HttpClient client, string name, string lastName)
    {
        var response = await client.PostAsJsonAsync("/api/v1/suppliers", new
        {
            name,
            lastName,
            ruc = "20123456789",
            email = "proveedor@test.local",
            phone = "999999999",
            address = "Av. Siempre Viva",
            contactPerson = "Contacto",
            category = "ABARROTES"
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CreatePurchaseOrderAsync(HttpClient client, int supplierId, int productId, int quantity)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await client.PostAsJsonAsync("/api/v1/purchases", new
        {
            supplierId,
            date = today,
            expectedDate = today.AddDays(7),
            currency = "PEN",
            description = "orden de prueba",
            lines = new[] { new { productId, quantity, unitPrice = 5m, discount = 0m } }
        });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }
}

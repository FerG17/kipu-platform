using System.Net.Http.Json;
using ClosedXML.Excel;
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

        var rows = await GetStockMovementRowsAsync(client, supplierId);

        Assert.Contains(rows, row => row.Type == "Entrada" && row.Quantity == 15);
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

        var rows = await GetStockMovementRowsAsync(client, wantedSupplierId);

        Assert.Contains(rows, row => row.Quantity == 7);
        Assert.DoesNotContain(rows, row => row.Quantity == 99);
    }

    /// <summary>Generates and exports a STOCK_MOVEMENTS report as .xlsx, then reads back its data rows (below the title/header rows — see ExcelReportGenerator).</summary>
    private static async Task<List<(string Type, int Quantity)>> GetStockMovementRowsAsync(HttpClient client, int supplierId)
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

        var exportResponse = await client.GetAsync($"/api/v1/reports/{reportId}/export/excel");
        exportResponse.EnsureSuccessStatusCode();
        var bytes = await exportResponse.Content.ReadAsByteArrayAsync();

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("Entradas y salidas");

        // Row 4 is the header ("Fecha", "Producto", "Tipo", "Cantidad", ...);
        // data starts at row 5. Column 3 = Tipo, column 4 = Cantidad.
        const int headerRow = 4;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;

        var rows = new List<(string Type, int Quantity)>();
        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            var quantityCell = sheet.Cell(row, 4);
            if (quantityCell.IsEmpty()) continue;
            rows.Add((sheet.Cell(row, 3).GetString(), quantityCell.GetValue<int>()));
        }

        return rows;
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

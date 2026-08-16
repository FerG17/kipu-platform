using ClosedXML.Excel;
using Kipu.Platform.Products.Interfaces.Acl;
using Kipu.Platform.Sales.Interfaces.Acl;

namespace Kipu.Platform.Dashboard.Infrastructure.Reporting;

/// <summary>
///     Renders each report type as a real, formatted .xlsx workbook — bold
///     header row, frozen so it stays visible while scrolling, real
///     date/number cell types (not text) so totals and pivots work in Excel
///     without the owner reformatting every column first, and autosized
///     columns. Replaces the old plain CSV (see the removed CsvWriter),
///     which opened as an unstyled wall of text with English column names.
///
///     Spanish headers are hardcoded rather than routed through
///     IStringLocalizer, matching StockMovementsPdfGenerator next to it —
///     these are downloadable business documents for the bodega's own
///     records, not UI chrome that should track the request's Accept-Language.
/// </summary>
public static class ExcelReportGenerator
{
    private const string HeaderFill = "#2B2620";
    private const string HeaderFont = "#FFFAF1";

    public static byte[] GenerateSales(int reportId, DateOnly? dateFrom, DateOnly? dateTo,
        IReadOnlyCollection<SaleExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Ventas");

        WriteTitle(sheet, $"Reporte de ventas #{reportId}", BuildDateRangeSummary(dateFrom, dateTo));

        var headerRow = 4;
        WriteHeader(sheet, headerRow, "N° venta", "Fecha", "Método de pago", "Total", "Moneda");

        var row = headerRow + 1;
        foreach (var line in rows)
        {
            sheet.Cell(row, 1).Value = line.SaleId;
            // .DateTime (not .LocalDateTime/.UtcDateTime): keeps the exact
            // same wall-clock value StockMovementsPdfGenerator's ToString()
            // already displays, whatever offset it was stored with — no new
            // timezone conversion introduced here.
            sheet.Cell(row, 2).Value = line.Date.DateTime;
            sheet.Cell(row, 2).Style.DateFormat.Format = "yyyy-mm-dd HH:mm";
            WriteText(sheet.Cell(row, 3), PaymentMethodLabel(line.PaymentMethod));
            sheet.Cell(row, 4).Value = line.TotalAmount;
            sheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            WriteText(sheet.Cell(row, 5), line.Currency);
            row++;
        }

        FinishSheet(sheet, headerRow, lastColumn: 5, hasRows: rows.Count > 0, emptyMessage: "Sin ventas en el rango seleccionado.");
        return ToBytes(workbook);
    }

    public static byte[] GenerateInventory(int reportId, IReadOnlyCollection<TopStockProductInfo> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Inventario");

        WriteTitle(sheet, $"Reporte de inventario #{reportId}", "Existencias actuales (no filtra por fecha)");

        var headerRow = 4;
        WriteHeader(sheet, headerRow, "ID producto", "Producto", "Stock actual");

        var row = headerRow + 1;
        foreach (var line in rows)
        {
            sheet.Cell(row, 1).Value = line.ProductId;
            WriteText(sheet.Cell(row, 2), line.ProductName);
            sheet.Cell(row, 3).Value = line.TotalStock;
            row++;
        }

        FinishSheet(sheet, headerRow, lastColumn: 3, hasRows: rows.Count > 0, emptyMessage: "Sin productos registrados.");
        return ToBytes(workbook);
    }

    public static byte[] GenerateStockMovements(int reportId, DateOnly? dateFrom, DateOnly? dateTo,
        string? productFilterName, string? supplierFilterName, IReadOnlyCollection<StockMovementReportLine> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Entradas y salidas");

        var summaryParts = new List<string>();
        var dateSummary = BuildDateRangeSummary(dateFrom, dateTo);
        if (dateSummary != null) summaryParts.Add(dateSummary);
        if (!string.IsNullOrEmpty(productFilterName)) summaryParts.Add($"Producto: {productFilterName}");
        if (!string.IsNullOrEmpty(supplierFilterName)) summaryParts.Add($"Proveedor: {supplierFilterName}");

        WriteTitle(sheet, $"Reporte de entradas/salidas #{reportId}",
            summaryParts.Count == 0 ? "Sin filtros (todas las entradas/salidas)" : string.Join("  ·  ", summaryParts));

        var headerRow = 4;
        WriteHeader(sheet, headerRow, "Fecha", "Producto", "Tipo", "Cantidad", "Proveedor", "Nota");

        var row = headerRow + 1;
        foreach (var line in rows)
        {
            sheet.Cell(row, 1).Value = line.RegisteredAt.DateTime;
            sheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd HH:mm";
            WriteText(sheet.Cell(row, 2), line.ProductName);
            WriteText(sheet.Cell(row, 3), line.Type == "INTAKE" ? "Entrada" : "Salida");
            sheet.Cell(row, 4).Value = line.Quantity;
            WriteText(sheet.Cell(row, 5), line.Supplier);
            WriteText(sheet.Cell(row, 6), line.Note);
            row++;
        }

        FinishSheet(sheet, headerRow, lastColumn: 6, hasRows: rows.Count > 0, emptyMessage: "Sin movimientos en el rango/filtro seleccionado.");
        return ToBytes(workbook);
    }

    private static void WriteTitle(IXLWorksheet sheet, string title, string? subtitle)
    {
        sheet.Cell(1, 1).Value = title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        sheet.Cell(2, 1).Value = subtitle ?? "Sin filtros";
        sheet.Cell(2, 1).Style.Font.FontSize = 9;
        sheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#7D7466");

        sheet.Cell(3, 1).Value = $"Generado: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC";
        sheet.Cell(3, 1).Style.Font.FontSize = 8;
        sheet.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#B1A695");
    }

    private static void WriteHeader(IXLWorksheet sheet, int headerRow, params string[] columns)
    {
        for (var i = 0; i < columns.Length; i++)
        {
            var cell = sheet.Cell(headerRow, i + 1);
            cell.Value = columns[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.FromHtml(HeaderFont);
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderFill);
        }
    }

    private static void FinishSheet(IXLWorksheet sheet, int headerRow, int lastColumn, bool hasRows, string emptyMessage)
    {
        if (!hasRows)
        {
            sheet.Cell(headerRow + 1, 1).Value = emptyMessage;
            sheet.Cell(headerRow + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#7D7466");
        }

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns(1, lastColumn).AdjustToContents();
    }

    private static string? BuildDateRangeSummary(DateOnly? dateFrom, DateOnly? dateTo)
    {
        if (!dateFrom.HasValue && !dateTo.HasValue) return null;
        return $"Fechas: {dateFrom?.ToString("yyyy-MM-dd") ?? "…"} a {dateTo?.ToString("yyyy-MM-dd") ?? "…"}";
    }

    private static string PaymentMethodLabel(string paymentMethod)
    {
        return paymentMethod switch
        {
            "CASH" => "Efectivo",
            "CARD" => "Tarjeta",
            "YAPE" => "Yape",
            "PLIN" => "Plin",
            _ => paymentMethod
        };
    }

    /// <summary>
    ///     Every free-text cell (product/supplier names, notes, payment
    ///     method) goes through here rather than a plain `.Value =` — two
    ///     layers, both needed:
    ///
    ///     1. It's a plain string assignment, never `.FormulaA1` — ClosedXML
    ///     only ever emits a formula element (`&lt;f&gt;`) when a formula
    ///     property is explicitly set, so a product named
    ///     "=HYPERLINK(...)" is written as inert text on first open, unlike
    ///     CSV, whose format has no way to mark a cell "this is definitely
    ///     text" and leaves that guess to whatever opens it.
    ///
    ///     2. Explicit Text number format ("@"). Without it, a cell holding
    ///     that same string is still stored as "General" — and Excel
    ///     re-evaluates a General-format cell's content when the owner
    ///     simply clicks in and confirms it (F2, Enter) with no edit at all,
    ///     turning safe-on-open into a live formula. "@" tells Excel this
    ///     cell is text permanently, not just on the read that already
    ///     happened.
    /// </summary>
    private static void WriteText(IXLCell cell, string text)
    {
        cell.Value = text;
        cell.Style.NumberFormat.Format = "@";
    }

    private static byte[] ToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Kipu.Platform.Products.Interfaces.Acl;
using Kipu.Platform.Shared.Application;

namespace Kipu.Platform.Dashboard.Infrastructure.Reporting;

/// <summary>
///     Renders the "entradas/salidas" (stock movements) report as PDF. Kept
///     as a plain static generator rather than a QuestPDF IDocument class —
///     no state to carry between calls, and it keeps the fluent layout in
///     one place instead of split across interface members.
/// </summary>
public static class StockMovementsPdfGenerator
{
    public static byte[] Generate(int reportId, DateOnly? dateFrom, DateOnly? dateTo, string? productFilterName,
        string? supplierFilterName, IReadOnlyCollection<StockMovementReportLine> lines, IBusinessClock businessClock)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text($"Reporte de entradas/salidas #{reportId}").FontSize(16).Bold();
                    column.Item().PaddingTop(2)
                        .Text(BuildFilterSummary(dateFrom, dateTo, productFilterName, supplierFilterName))
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                    column.Item().PaddingTop(2)
                        .Text($"Generado: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Fecha").Bold();
                        header.Cell().Text("Producto").Bold();
                        header.Cell().Text("Tipo").Bold();
                        header.Cell().Text("Cantidad").Bold();
                        header.Cell().Text("Proveedor").Bold();
                        header.Cell().Text("Nota").Bold();
                    });

                    foreach (var line in lines)
                    {
                        table.Cell().Text(businessClock.ToLocalDateTime(line.RegisteredAt).ToString("yyyy-MM-dd HH:mm"));
                        table.Cell().Text(line.ProductName);
                        table.Cell().Text(line.Type == "INTAKE" ? "Entrada" : "Salida");
                        table.Cell().Text(line.Quantity.ToString());
                        table.Cell().Text(line.Supplier);
                        table.Cell().Text(line.Note);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string BuildFilterSummary(DateOnly? dateFrom, DateOnly? dateTo, string? productFilterName,
        string? supplierFilterName)
    {
        var parts = new List<string>();
        if (dateFrom.HasValue || dateTo.HasValue)
            parts.Add($"Fechas: {dateFrom?.ToString("yyyy-MM-dd") ?? "…"} a {dateTo?.ToString("yyyy-MM-dd") ?? "…"}");
        if (!string.IsNullOrEmpty(productFilterName))
            parts.Add($"Producto: {productFilterName}");
        if (!string.IsNullOrEmpty(supplierFilterName))
            parts.Add($"Proveedor: {supplierFilterName}");

        return parts.Count == 0 ? "Sin filtros (todas las entradas/salidas)" : string.Join("  ·  ", parts);
    }
}

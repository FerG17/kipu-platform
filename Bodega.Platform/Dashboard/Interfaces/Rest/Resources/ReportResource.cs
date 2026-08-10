namespace Bodega.Platform.Dashboard.Interfaces.Rest.Resources;

public record ReportResource(int Id, int BusinessId, string Type, DateOnly? DateFrom, DateOnly? DateTo, int? ProductId,
    int? SupplierId, DateTimeOffset GeneratedAt);

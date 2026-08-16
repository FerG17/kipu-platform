namespace Kipu.Platform.Dashboard.Interfaces.Rest.Resources;

public record GenerateReportResource(string Type, DateOnly? DateFrom, DateOnly? DateTo, int? ProductId = null,
    int? SupplierId = null);

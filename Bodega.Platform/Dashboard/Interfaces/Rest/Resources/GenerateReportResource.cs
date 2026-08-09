namespace Bodega.Platform.Dashboard.Interfaces.Rest.Resources;

public record GenerateReportResource(string Type, DateOnly? DateFrom, DateOnly? DateTo);

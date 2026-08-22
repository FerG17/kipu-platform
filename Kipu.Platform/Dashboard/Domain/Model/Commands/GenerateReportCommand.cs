namespace Kipu.Platform.Dashboard.Domain.Model.Commands;

/// <summary>Persists the report's metadata (for history) and returns it — the actual figures are computed live, not stored (see Report.cs).</summary>
public record GenerateReportCommand(int BusinessId, string Type, DateOnly? DateFrom, DateOnly? DateTo, int? ProductId,
    int? SupplierId);

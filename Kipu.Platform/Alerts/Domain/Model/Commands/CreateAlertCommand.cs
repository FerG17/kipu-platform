namespace Kipu.Platform.Alerts.Domain.Model.Commands;

/// <summary>
///     Manual/technical alert creation — not the normal path (the engine
///     creates alerts reactively via event handlers), but kept as a public
///     command for admin/testing use and the official Gherkin scenario
///     (see architecture doc §6.5).
/// </summary>
public record CreateAlertCommand(
    int BusinessId,
    int ProductId,
    int? BatchId,
    string ProductName,
    string Type,
    string Severity,
    string Message,
    decimal CurrentStock,
    decimal MinStock,
    int? DaysToExpiry,
    int? WarehouseId = null);

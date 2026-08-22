namespace Kipu.Platform.Alerts.Interfaces.Rest.Resources;

public record CreateAlertResource(
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

namespace Bodega.Platform.Alerts.Interfaces.Rest.Resources;

public record CreateAlertResource(
    int ProductId,
    int? BatchId,
    string ProductName,
    string Type,
    string Severity,
    string Message,
    int CurrentStock,
    int MinStock,
    int? DaysToExpiry,
    int? WarehouseId = null);

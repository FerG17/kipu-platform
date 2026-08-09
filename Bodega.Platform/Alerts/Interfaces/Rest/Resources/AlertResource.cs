namespace Bodega.Platform.Alerts.Interfaces.Rest.Resources;

public record AlertResource(
    int Id,
    int BusinessId,
    int ProductId,
    int? BatchId,
    int? WarehouseId,
    string ProductName,
    string Type,
    string Severity,
    string Message,
    string Status,
    DateTimeOffset Date,
    int CurrentStock,
    int MinStock,
    int? DaysToExpiry,
    bool Notified,
    DateTimeOffset? NotifiedAt,
    DateTimeOffset? ResolvedAt);

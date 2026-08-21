namespace Kipu.Platform.Alerts.Domain.Model.Aggregates;

public static class AlertType
{
    public const string LowStock = "LOW_STOCK";
    public const string OutOfStock = "OUT_OF_STOCK";
    public const string Expiration = "EXPIRATION";
    public const string Expired = "EXPIRED";
}

public static class AlertSeverity
{
    public const string High = "HIGH";
    public const string Medium = "MEDIUM";
    public const string Low = "LOW";
}

public static class AlertStatus
{
    public const string Active = "ACTIVE";
    public const string Acknowledged = "ACKNOWLEDGED";
    public const string Sent = "SENT";
    public const string Resolved = "RESOLVED";
}

/// <summary>
///     A live operational alert (low stock, out of stock, expiring soon, or
///     expired). Always persisted server-side — unlike the frontend's
///     original in-memory-only synthesis (`id: null`), this is now the
///     source of truth (see architecture doc §5.4).
///
///     Resolved alerts are pure, immutable history — never recalculated once
///     RESOLVED (see Resolve()).
/// </summary>
public class Alert(
    int businessId,
    int productId,
    int? batchId,
    string productName,
    string type,
    string severity,
    string message,
    int currentStock,
    int minStock,
    int? daysToExpiry,
    int? warehouseId = null)
{
    public Alert() : this(0, 0, null, string.Empty, AlertType.LowStock, AlertSeverity.Low, string.Empty, 0, 0, null)
    {
    }

    public int Id { get; }
    public int BusinessId { get; private set; } = businessId;
    public int ProductId { get; private set; } = productId;
    public int? BatchId { get; private set; } = batchId;

    /// <summary>
    ///     Which warehouse this LOW_STOCK/OUT_OF_STOCK alert is about — a
    ///     product split across warehouses can be critically low in one and
    ///     perfectly healthy in another, so the alert must say which one
    ///     instead of reading as a business-wide problem. Null for
    ///     EXPIRATION/EXPIRED alerts, which are scoped by BatchId instead.
    /// </summary>
    public int? WarehouseId { get; private set; } = warehouseId;

    public string ProductName { get; private set; } = productName;
    public string Type { get; private set; } = type;
    public string Severity { get; private set; } = severity;
    public string Message { get; private set; } = message;
    public string Status { get; private set; } = AlertStatus.Active;
    public DateTimeOffset Date { get; private set; } = DateTimeOffset.UtcNow;
    public int CurrentStock { get; private set; } = currentStock;
    public int MinStock { get; private set; } = minStock;
    public int? DaysToExpiry { get; private set; } = daysToExpiry;
    public bool Notified { get; private set; }
    public DateTimeOffset? NotifiedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Refreshes the snapshot fields of a still-ACTIVE alert as stock keeps changing — a no-op once RESOLVED.</summary>
    public Alert RefreshStockInfo(string severity, string message, int currentStock)
    {
        if (Status == AlertStatus.Resolved) return this;

        Severity = severity;
        Message = message;
        CurrentStock = currentStock;
        Date = DateTimeOffset.UtcNow;
        return this;
    }

    public Alert Acknowledge()
    {
        Status = AlertStatus.Acknowledged;
        return this;
    }

    public Alert Resolve()
    {
        Status = AlertStatus.Resolved;
        ResolvedAt = DateTimeOffset.UtcNow;
        return this;
    }
}

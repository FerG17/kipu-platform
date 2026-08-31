namespace Kipu.Platform.Alerts.Domain.Model.Aggregates;

public static class AlertType
{
    public const string LowStock = "LOW_STOCK";
    public const string OutOfStock = "OUT_OF_STOCK";
    public const string Expiration = "EXPIRATION";
    public const string Expired = "EXPIRED";

    /// <summary>A credit sale's next cuota is coming due (X6 #7) — no product involved, see Alert.ForInstallmentDue.</summary>
    public const string InstallmentDue = "INSTALLMENT_DUE";

    /// <summary>A credit purchase order's next cuota is coming due (X6 #12, Bloque G2) — reserved now so the schema migration covers both alert types in one pass.</summary>
    public const string SupplierInstallmentDue = "SUPPLIER_INSTALLMENT_DUE";
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
///     A live operational alert — low stock, out of stock, expiring soon,
///     expired, or (X6 #7) a credit installment coming due. Always persisted
///     server-side — unlike the frontend's original in-memory-only synthesis
///     (`id: null`), this is now the source of truth (see architecture doc
///     §5.4).
///
///     Resolved alerts are pure, immutable history — never recalculated once
///     RESOLVED (see Resolve()).
///
///     ProductId/ProductName/CurrentStock/MinStock are nullable because they
///     only apply to the inventory alert types (LOW_STOCK/OUT_OF_STOCK/
///     EXPIRATION/EXPIRED) — an INSTALLMENT_DUE/SUPPLIER_INSTALLMENT_DUE
///     alert has no product at all, and populates SaleId/PurchaseOrderId/
///     CustomerOrSupplierName/Amount/DaysRemaining instead. Use the
///     constructor directly for inventory alerts, ForInstallmentDue for
///     installment ones — the two shapes don't overlap enough to share one
///     positional constructor without every inventory call site having to
///     pass a run of nulls it doesn't otherwise need.
/// </summary>
public class Alert
{
    public Alert(
        int businessId,
        int? productId,
        int? batchId,
        string? productName,
        string type,
        string severity,
        string message,
        decimal? currentStock,
        decimal? minStock,
        int? daysToExpiry,
        int? warehouseId = null)
    {
        BusinessId = businessId;
        ProductId = productId;
        BatchId = batchId;
        ProductName = productName;
        Type = type;
        Severity = severity;
        Message = message;
        CurrentStock = currentStock;
        MinStock = minStock;
        DaysToExpiry = daysToExpiry;
        WarehouseId = warehouseId;
    }

    public Alert() : this(0, 0, null, string.Empty, AlertType.LowStock, AlertSeverity.Low, string.Empty, 0, 0, null)
    {
    }

    /// <summary>Builds an INSTALLMENT_DUE alert for a credit sale's next unpaid cuota — see the class doc comment.</summary>
    public static Alert ForInstallmentDue(int businessId, int saleId, string? customerName, string severity,
        string message, decimal amount, int daysRemaining)
    {
        return new Alert(businessId, null, null, null, AlertType.InstallmentDue, severity, message, null, null, null)
        {
            SaleId = saleId,
            CustomerOrSupplierName = customerName,
            Amount = amount,
            DaysRemaining = daysRemaining
        };
    }

    public int Id { get; }
    public int BusinessId { get; private set; }
    public int? ProductId { get; private set; }
    public int? BatchId { get; private set; }

    /// <summary>
    ///     Which warehouse this LOW_STOCK/OUT_OF_STOCK alert is about — a
    ///     product split across warehouses can be critically low in one and
    ///     perfectly healthy in another, so the alert must say which one
    ///     instead of reading as a business-wide problem. Null for
    ///     EXPIRATION/EXPIRED/INSTALLMENT_DUE alerts.
    /// </summary>
    public int? WarehouseId { get; private set; }

    public string? ProductName { get; private set; }
    public string Type { get; private set; } = AlertType.LowStock;
    public string Severity { get; private set; } = AlertSeverity.Low;
    public string Message { get; private set; } = string.Empty;
    public string Status { get; private set; } = AlertStatus.Active;
    public DateTimeOffset Date { get; private set; } = DateTimeOffset.UtcNow;
    public decimal? CurrentStock { get; private set; }
    public decimal? MinStock { get; private set; }
    public int? DaysToExpiry { get; private set; }
    public bool Notified { get; private set; }
    public DateTimeOffset? NotifiedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>The credit sale this INSTALLMENT_DUE alert is about — null for every other alert type.</summary>
    public int? SaleId { get; private set; }

    /// <summary>The credit purchase order this SUPPLIER_INSTALLMENT_DUE alert is about (X6 #12, Bloque G2) — null for every other alert type.</summary>
    public int? PurchaseOrderId { get; private set; }

    /// <summary>The customer (sale) or supplier (purchase order) this cuota belongs to — null for an anonymous sale, or for any non-installment alert.</summary>
    public string? CustomerOrSupplierName { get; private set; }

    /// <summary>The due cuota's amount — null for every non-installment alert type.</summary>
    public decimal? Amount { get; private set; }

    /// <summary>Days until (positive) or since (negative/zero) the cuota's DueDate — null for every non-installment alert type.</summary>
    public int? DaysRemaining { get; private set; }

    /// <summary>Refreshes the snapshot fields of a still-ACTIVE inventory alert as stock keeps changing — a no-op once RESOLVED.</summary>
    public Alert RefreshStockInfo(string severity, string message, decimal currentStock)
    {
        if (Status == AlertStatus.Resolved) return this;

        Severity = severity;
        Message = message;
        CurrentStock = currentStock;
        Date = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>Refreshes the snapshot fields of a still-ACTIVE installment alert as the due date approaches — a no-op once RESOLVED.</summary>
    public Alert RefreshInstallmentInfo(string severity, string message, int daysRemaining)
    {
        if (Status == AlertStatus.Resolved) return this;

        Severity = severity;
        Message = message;
        DaysRemaining = daysRemaining;
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

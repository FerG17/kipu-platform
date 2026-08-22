namespace Kipu.Platform.Alerts.Domain.Model.Entities;

/// <summary>
///     Configurable per-business threshold, replacing the frontend's
///     decorative "Reglas" tab and the hardcoded "7 days" constant (see
///     architecture doc §5.4). ThresholdValue is only meaningful for
///     AlertType.Expiration (days before expiry to warn) — for LOW_STOCK/
///     OUT_OF_STOCK, only Enabled matters (the real threshold is each
///     product's own MinimumStock).
/// </summary>
public class AlertRule(int businessId, string alertType, int thresholdValue, bool enabled)
{
    public AlertRule() : this(0, string.Empty, 0, true)
    {
    }

    public int Id { get; }
    public int BusinessId { get; private set; } = businessId;
    public string AlertType { get; private set; } = alertType;
    public int ThresholdValue { get; private set; } = thresholdValue;
    public bool Enabled { get; private set; } = enabled;

    public AlertRule UpdateDetails(int thresholdValue, bool enabled)
    {
        ThresholdValue = thresholdValue;
        Enabled = enabled;
        return this;
    }
}

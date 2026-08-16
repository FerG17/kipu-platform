namespace Kipu.Platform.Alerts.Interfaces.Acl;

/// <summary>
///     The only way another bounded context may reach into Alerts &amp;
///     Operational Monitoring — never direct repository/DbContext access.
/// </summary>
public interface IAlertsContextFacade
{
    /// <summary>
    ///     How many days before expiry this business wants to be warned
    ///     (AlertRule for EXPIRATION), falling back to the platform default
    ///     when it has not configured one.
    ///
    ///     Exposed because the batch resource reports isExpiringSoon/
    ///     daysToExpiry to clients, and it used to compute that against the
    ///     hardcoded 7-day default. A shop that set its threshold to 30 days
    ///     therefore saw isExpiringSoon:false on a batch that already had a
    ///     live EXPIRATION alert against it.
    /// </summary>
    Task<int> GetExpirationThresholdDays(int businessId, CancellationToken cancellationToken);
}

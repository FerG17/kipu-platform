namespace Bodega.Platform.Alerts.Domain.Model.Errors;

public enum AlertsError
{
    AlertNotFound,
    AlertAlreadyResolved,
    InvalidThreshold,
    InvalidAlertData,
    DatabaseError
}

using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Interfaces.Rest.Resources;

namespace Kipu.Platform.Alerts.Interfaces.Rest.Transform;

public static class AlertResourceFromEntityAssembler
{
    public static AlertResource ToResourceFromEntity(Alert alert)
    {
        return new AlertResource(alert.Id, alert.BusinessId, alert.ProductId, alert.BatchId, alert.WarehouseId, alert.ProductName,
            alert.Type, alert.Severity, alert.Message, alert.Status, alert.Date, alert.CurrentStock, alert.MinStock,
            alert.DaysToExpiry, alert.Notified, alert.NotifiedAt, alert.ResolvedAt, alert.SaleId, alert.PurchaseOrderId,
            alert.CustomerOrSupplierName, alert.Amount, alert.DaysRemaining);
    }
}

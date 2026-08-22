namespace Kipu.Platform.Suppliers.Interfaces.Rest.Resources;

public record PurchaseOrderResource(
    int Id,
    int BusinessId,
    int SupplierId,
    DateOnly Date,
    DateOnly? ExpectedDate,
    DateOnly? ReceivedDate,
    string Status,
    string Currency,
    string Description,
    IReadOnlyCollection<PurchaseOrderDetailResource> Details);

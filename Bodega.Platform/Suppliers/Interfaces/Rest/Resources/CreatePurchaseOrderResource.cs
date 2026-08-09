namespace Bodega.Platform.Suppliers.Interfaces.Rest.Resources;

public record CreatePurchaseOrderResource(
    int SupplierId,
    DateOnly Date,
    DateOnly? ExpectedDate,
    string Currency,
    string Description,
    IReadOnlyCollection<PurchaseOrderLineResource> Lines);

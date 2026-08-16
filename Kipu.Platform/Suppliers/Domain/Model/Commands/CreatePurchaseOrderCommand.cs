namespace Kipu.Platform.Suppliers.Domain.Model.Commands;

public record CreatePurchaseOrderCommand(
    int BusinessId,
    int SupplierId,
    DateOnly Date,
    DateOnly? ExpectedDate,
    string Currency,
    string Description,
    IReadOnlyCollection<PurchaseOrderLineCommand> Lines);

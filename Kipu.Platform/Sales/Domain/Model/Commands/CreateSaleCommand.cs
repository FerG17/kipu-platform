namespace Kipu.Platform.Sales.Domain.Model.Commands;

public record CreateSaleCommand(
    int BusinessId,
    int? CustomerId,
    string PaymentMethod,
    string Currency,
    string Description,
    IReadOnlyCollection<SaleLineCommand> Lines);

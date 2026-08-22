namespace Kipu.Platform.Sales.Domain.Model.Commands;

public record SaleLineCommand(int ProductId, decimal Quantity);

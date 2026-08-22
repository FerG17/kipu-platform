namespace Kipu.Platform.Products.Domain.Model.Commands;

/// <summary>
///     Puts units back on the shelf after a sale is cancelled — the mirror of
///     RegisterStockSaleCommand. Not exposed as its own REST endpoint; Sales
///     reaches it through IProductContextFacade when a sale is cancelled.
/// </summary>
public record RegisterStockReturnCommand(int ProductId, int BusinessId, decimal Quantity);

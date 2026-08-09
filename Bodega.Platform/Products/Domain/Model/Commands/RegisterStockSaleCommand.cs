namespace Bodega.Platform.Products.Domain.Model.Commands;

/// <summary>
///     Decrements inventory after a confirmed sale. Not exposed as its own
///     REST endpoint — invoked by Sales & POS via IProductContextFacade (or,
///     once Sales exists, by an event handler reacting to a domain event it
///     raises), never called directly by the frontend.
/// </summary>
public record RegisterStockSaleCommand(int ProductId, int BusinessId, int Quantity);

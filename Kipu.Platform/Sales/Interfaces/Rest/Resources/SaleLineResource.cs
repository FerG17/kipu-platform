namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

/// <summary>
///     No UnitPrice field, deliberately: the server always prices a line from
///     the product's own current BasePrice (see SaleCommandService), never
///     from client input — a UnitPrice here used to look editable from the
///     wire shape alone while silently being ignored server-side the whole
///     time.
/// </summary>
public record SaleLineResource(int ProductId, int Quantity);

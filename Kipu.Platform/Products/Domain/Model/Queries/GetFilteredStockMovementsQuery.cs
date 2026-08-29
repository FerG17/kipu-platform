namespace Kipu.Platform.Products.Domain.Model.Queries;

/// <summary>
///     Backs Kardex: an unpaginated, filterable movement list. Ascending +
///     unit cost (from each movement's Batch) is what turns this into a
///     running-balance kardex once a single product is selected on the
///     frontend — see StockMovementResource.UnitCost.
/// </summary>
public record GetFilteredStockMovementsQuery(
    int BusinessId,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int? ProductId,
    string? Category,
    bool Ascending);

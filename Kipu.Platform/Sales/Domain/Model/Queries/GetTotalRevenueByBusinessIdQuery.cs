namespace Kipu.Platform.Sales.Domain.Model.Queries;

/// <summary>
///     The single source of truth for "total revenue" — Dashboard, POS and
///     Reports must all call this instead of each computing their own sum
///     (the exact bug the handoff doc flagged: "cada pantalla lo calculaba distinto").
/// </summary>
public record GetTotalRevenueByBusinessIdQuery(int BusinessId, DateOnly? DateFrom = null, DateOnly? DateTo = null);

namespace Kipu.Platform.Dashboard.Domain.Model.Queries;

/// <summary>Defaults to the last 7 days when DateFrom/DateTo are omitted (weekly chart, per architecture doc §6.8).</summary>
public record GetSalesByDayQuery(int BusinessId, DateOnly? DateFrom = null, DateOnly? DateTo = null);

public record SalesByDayResult(DateOnly Date, decimal Total);

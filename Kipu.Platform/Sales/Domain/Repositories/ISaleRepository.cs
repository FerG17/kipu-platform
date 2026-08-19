using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Sales.Domain.Repositories;

public interface ISaleRepository : IBaseRepository<Sale>
{
    Task<IEnumerable<Sale>> FindAllByBusinessIdAsync(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken cancellationToken = default);

    /// <summary>FindByIdAsync (from IBaseRepository) does not eager-load SaleDetails — use this when the lines are needed.</summary>
    Task<Sale?> FindByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<decimal> SumPaidTotalByBusinessIdAsync(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken cancellationToken = default);

    /// <summary>Used to make CreateSaleCommand idempotent — see Sale.IdempotencyKey.</summary>
    Task<Sale?> FindByBusinessIdAndIdempotencyKeyAsync(int businessId, string idempotencyKey,
        CancellationToken cancellationToken = default);
}

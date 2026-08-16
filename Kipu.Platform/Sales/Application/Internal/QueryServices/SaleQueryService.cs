using Kipu.Platform.Sales.Application.QueryServices;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Queries;
using Kipu.Platform.Sales.Domain.Repositories;

namespace Kipu.Platform.Sales.Application.Internal.QueryServices;

public class SaleQueryService(ISaleRepository saleRepository) : ISaleQueryService
{
    public async Task<IEnumerable<Sale>> Handle(GetAllSalesByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await saleRepository.FindAllByBusinessIdAsync(query.BusinessId, query.DateFrom, query.DateTo, cancellationToken);
    }

    public async Task<Sale?> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken)
    {
        return await saleRepository.FindByIdWithDetailsAsync(query.SaleId, cancellationToken);
    }

    public async Task<decimal> Handle(GetTotalRevenueByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await saleRepository.SumPaidTotalByBusinessIdAsync(query.BusinessId, query.DateFrom, query.DateTo, cancellationToken);
    }
}

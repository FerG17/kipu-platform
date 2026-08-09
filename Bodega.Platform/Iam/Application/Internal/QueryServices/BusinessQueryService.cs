using Bodega.Platform.Iam.Application.QueryServices;
using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Domain.Model.Queries;
using Bodega.Platform.Iam.Domain.Repositories;

namespace Bodega.Platform.Iam.Application.Internal.QueryServices;

public class BusinessQueryService(IBusinessRepository businessRepository) : IBusinessQueryService
{
    public async Task<Business?> Handle(GetBusinessByIdQuery query, CancellationToken cancellationToken)
    {
        return await businessRepository.FindByIdAsync(query.BusinessId, cancellationToken);
    }
}

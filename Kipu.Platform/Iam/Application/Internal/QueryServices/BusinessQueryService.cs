using Kipu.Platform.Iam.Application.QueryServices;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Model.Queries;
using Kipu.Platform.Iam.Domain.Repositories;

namespace Kipu.Platform.Iam.Application.Internal.QueryServices;

public class BusinessQueryService(IBusinessRepository businessRepository) : IBusinessQueryService
{
    public async Task<Business?> Handle(GetBusinessByIdQuery query, CancellationToken cancellationToken)
    {
        return await businessRepository.FindByIdAsync(query.BusinessId, cancellationToken);
    }
}

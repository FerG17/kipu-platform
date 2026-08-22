using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Model.Queries;

namespace Kipu.Platform.Iam.Application.QueryServices;

public interface IBusinessQueryService
{
    Task<Business?> Handle(GetBusinessByIdQuery query, CancellationToken cancellationToken);
}

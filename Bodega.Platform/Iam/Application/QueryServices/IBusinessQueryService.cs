using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Domain.Model.Queries;

namespace Bodega.Platform.Iam.Application.QueryServices;

public interface IBusinessQueryService
{
    Task<Business?> Handle(GetBusinessByIdQuery query, CancellationToken cancellationToken);
}

using Bodega.Platform.Iam.Domain.Model.Entities;
using Bodega.Platform.Iam.Domain.Model.Queries;

namespace Bodega.Platform.Iam.Application.QueryServices;

public interface IRoleQueryService
{
    Task<IEnumerable<Role>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken);
}

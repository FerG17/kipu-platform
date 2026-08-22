using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Domain.Model.Queries;

namespace Kipu.Platform.Iam.Application.QueryServices;

public interface IRoleQueryService
{
    Task<IEnumerable<Role>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken);
}

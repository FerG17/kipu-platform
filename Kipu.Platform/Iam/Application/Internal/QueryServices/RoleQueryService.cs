using Kipu.Platform.Iam.Application.QueryServices;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Domain.Model.Queries;
using Kipu.Platform.Iam.Domain.Repositories;

namespace Kipu.Platform.Iam.Application.Internal.QueryServices;

public class RoleQueryService(IRoleRepository roleRepository) : IRoleQueryService
{
    public async Task<IEnumerable<Role>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken)
    {
        return await roleRepository.ListAsync(cancellationToken);
    }
}

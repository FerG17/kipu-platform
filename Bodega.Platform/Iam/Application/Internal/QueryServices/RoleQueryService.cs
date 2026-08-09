using Bodega.Platform.Iam.Application.QueryServices;
using Bodega.Platform.Iam.Domain.Model.Entities;
using Bodega.Platform.Iam.Domain.Model.Queries;
using Bodega.Platform.Iam.Domain.Repositories;

namespace Bodega.Platform.Iam.Application.Internal.QueryServices;

public class RoleQueryService(IRoleRepository roleRepository) : IRoleQueryService
{
    public async Task<IEnumerable<Role>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken)
    {
        return await roleRepository.ListAsync(cancellationToken);
    }
}

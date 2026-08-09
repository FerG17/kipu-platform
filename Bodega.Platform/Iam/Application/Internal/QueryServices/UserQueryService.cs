using Bodega.Platform.Iam.Application.QueryServices;
using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Domain.Model.Queries;
using Bodega.Platform.Iam.Domain.Repositories;

namespace Bodega.Platform.Iam.Application.Internal.QueryServices;

public class UserQueryService(IUserRepository userRepository) : IUserQueryService
{
    public async Task<User?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        return await userRepository.FindByIdAsync(query.UserId, cancellationToken);
    }

    public async Task<User?> Handle(GetUserByEmailQuery query, CancellationToken cancellationToken)
    {
        return await userRepository.FindByEmailAsync(query.Email, cancellationToken);
    }

    public async Task<IEnumerable<User>> Handle(GetAllUsersByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await userRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }
}

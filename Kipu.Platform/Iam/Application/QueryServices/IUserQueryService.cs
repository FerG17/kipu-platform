using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Model.Queries;

namespace Kipu.Platform.Iam.Application.QueryServices;

public interface IUserQueryService
{
    Task<User?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken);
    Task<User?> Handle(GetUserByEmailQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<User>> Handle(GetAllUsersByBusinessIdQuery query, CancellationToken cancellationToken);
}

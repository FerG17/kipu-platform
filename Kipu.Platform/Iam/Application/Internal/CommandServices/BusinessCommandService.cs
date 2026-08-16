using Microsoft.Extensions.Localization;
using Kipu.Platform.Iam.Application.CommandServices;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Domain.Model.Errors;
using Kipu.Platform.Iam.Domain.Repositories;
using Kipu.Platform.Iam.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Iam.Application.Internal.CommandServices;

public class BusinessCommandService(
    IBusinessRepository businessRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<IamMessages> localizer)
    : IBusinessCommandService
{
    public async Task<Result<Business>> Handle(UpdateBusinessCommand command, CancellationToken cancellationToken)
    {
        var business = await businessRepository.FindByIdAsync(command.BusinessId, cancellationToken);
        if (business == null)
            return Result<Business>.Failure(IamError.BusinessNotFound, localizer[nameof(IamError.BusinessNotFound)]);

        business.UpdateProfile(command.Name, command.Type, command.Address, command.Ruc);

        businessRepository.Update(business);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Business>.Success(business);
    }
}

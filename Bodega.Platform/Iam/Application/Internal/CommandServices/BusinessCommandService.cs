using Microsoft.Extensions.Localization;
using Bodega.Platform.Iam.Application.CommandServices;
using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Iam.Domain.Model.Errors;
using Bodega.Platform.Iam.Domain.Repositories;
using Bodega.Platform.Iam.Resources;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Iam.Application.Internal.CommandServices;

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

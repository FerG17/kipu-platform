using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Sales.Domain.Model.Entities;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Sales.Application.CommandServices;

public interface IPaymentPlanCommandService
{
    Task<Result<PaymentPlan>> Handle(CreatePaymentPlanCommand command, CancellationToken cancellationToken);
    Task<Result<PaymentPlan>> Handle(RegisterInstallmentPaymentCommand command, CancellationToken cancellationToken);
}

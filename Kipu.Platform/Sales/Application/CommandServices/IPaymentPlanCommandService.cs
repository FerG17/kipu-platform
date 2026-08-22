using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Sales.Application.CommandServices;

public interface IPaymentPlanCommandService
{
    Task<Result<PaymentPlan>> Handle(CreatePaymentPlanCommand command, CancellationToken cancellationToken);
    Task<Result<PaymentPlan>> Handle(RegisterInstallmentPaymentCommand command, CancellationToken cancellationToken);
    Task<Result<PaymentPlan>> Handle(RevertInstallmentPaymentCommand command, CancellationToken cancellationToken);
}

using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Suppliers.Domain.Model.Commands;
using Kipu.Platform.Suppliers.Domain.Model.Entities;

namespace Kipu.Platform.Suppliers.Application.CommandServices;

public interface ISupplierPaymentPlanCommandService
{
    Task<Result<SupplierPaymentPlan>> Handle(CreateSupplierPaymentPlanCommand command, CancellationToken cancellationToken);
    Task<Result<SupplierPaymentPlan>> Handle(RegisterSupplierInstallmentPaymentCommand command, CancellationToken cancellationToken);
    Task<Result<SupplierPaymentPlan>> Handle(RevertSupplierInstallmentPaymentCommand command, CancellationToken cancellationToken);
    Task<Result<SupplierPaymentPlan>> Handle(UpdateSupplierPaymentInstallmentCommand command, CancellationToken cancellationToken);
}

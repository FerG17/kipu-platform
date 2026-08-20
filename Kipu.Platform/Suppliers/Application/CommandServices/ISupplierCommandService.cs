using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Commands;

namespace Kipu.Platform.Suppliers.Application.CommandServices;

public interface ISupplierCommandService
{
    Task<Result<Supplier>> Handle(CreateSupplierCommand command, CancellationToken cancellationToken);
    Task<Result<Supplier>> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken);
    Task<Result<Supplier>> Handle(DeactivateSupplierCommand command, CancellationToken cancellationToken);
    Task<Result<Supplier>> Handle(ReactivateSupplierCommand command, CancellationToken cancellationToken);
}

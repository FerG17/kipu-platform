using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Sales.Application.CommandServices;

public interface ICustomerCommandService
{
    Task<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken);
    Task<Result<Customer>> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteCustomerCommand command, CancellationToken cancellationToken);
}

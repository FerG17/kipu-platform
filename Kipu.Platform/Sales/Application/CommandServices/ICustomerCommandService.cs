using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Sales.Application.CommandServices;

public interface ICustomerCommandService
{
    Task<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken);
    Task<Result<Customer>> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteCustomerCommand command, CancellationToken cancellationToken);
}

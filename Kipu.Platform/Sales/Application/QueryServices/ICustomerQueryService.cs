using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Queries;

namespace Kipu.Platform.Sales.Application.QueryServices;

public interface ICustomerQueryService
{
    Task<IEnumerable<Customer>> Handle(GetAllCustomersByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Customer?> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken);
}

using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Sales.Application.QueryServices;

public interface ICustomerQueryService
{
    Task<PagedResult<Customer>> Handle(GetAllCustomersByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Customer?> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken);
}

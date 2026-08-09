using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Sales.Domain.Model.Queries;

namespace Bodega.Platform.Sales.Application.QueryServices;

public interface ICustomerQueryService
{
    Task<IEnumerable<Customer>> Handle(GetAllCustomersByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Customer?> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken);
}

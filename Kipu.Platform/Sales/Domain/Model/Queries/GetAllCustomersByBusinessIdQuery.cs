using Kipu.Platform.Shared.Domain.Model.Queries;

namespace Kipu.Platform.Sales.Domain.Model.Queries;

public record GetAllCustomersByBusinessIdQuery(int BusinessId, PageRequest Page);

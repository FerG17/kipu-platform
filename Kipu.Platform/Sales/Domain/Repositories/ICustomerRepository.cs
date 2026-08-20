using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Sales.Domain.Repositories;

public interface ICustomerRepository : IBaseRepository<Customer>
{
    Task<IEnumerable<Customer>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

    /// <summary>Looks across every customer for the business (active or not) — a document number shouldn't be reusable just because the row it belonged to was deactivated.</summary>
    Task<Customer?> FindByBusinessIdAndDocumentNumberAsync(int businessId, string documentNumber,
        CancellationToken cancellationToken = default);
}

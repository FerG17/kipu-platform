using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class CustomerRepository(AppDbContext context) : BaseRepository<Customer>(context), ICustomerRepository
{
    /// <summary>Excludes deactivated customers (see Customer.Deactivate, I31) — a "deleted" customer stops showing up in the picker, even though the row stays for its existing sales/payment plans to resolve against.</summary>
    public async Task<PagedResult<Customer>> FindAllByBusinessIdAsync(int businessId, PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Customer>().Where(customer => customer.BusinessId == businessId && customer.Status == CustomerStatus.Active);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(customer => customer.Id).Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<Customer>(items, totalCount, page.Page, page.PageSize);
    }

    public async Task<Customer?> FindByBusinessIdAndDocumentNumberAsync(int businessId, string documentNumber,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Customer>()
            .FirstOrDefaultAsync(customer => customer.BusinessId == businessId && customer.DocumentNumber == documentNumber,
                cancellationToken);
    }
}

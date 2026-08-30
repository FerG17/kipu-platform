using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Products.Application.CommandServices;

public interface ICategoryCommandService
{
    Task<Result<Category>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken);

    /// <summary>
    ///     Seeds the fixed vocabulary (Dairy/Grains/.../Other) for a
    ///     newly-registered business — mirrors CreateDefaultWarehouse,
    ///     called once from IAM's sign-up flow so a new business always has a
    ///     usable category catalog from its first login.
    /// </summary>
    Task SeedDefaultCategories(int businessId, CancellationToken cancellationToken);
}

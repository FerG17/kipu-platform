using FluentValidation;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Products.Application.CommandServices;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Domain.Model.Errors;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Products.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Application.Internal.CommandServices;

public class CategoryCommandService(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateCategoryCommand> createCategoryValidator,
    IStringLocalizer<ProductMessages> localizer)
    : ICategoryCommandService
{
    private static readonly string[] DefaultCategoryNames =
    [
        ProductCategory.Dairy, ProductCategory.Grains, ProductCategory.Oils, ProductCategory.Beverages,
        ProductCategory.Cleaning, ProductCategory.Medicine, ProductCategory.Other
    ];

    public async Task<Result<Category>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        if (!(await createCategoryValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Category>.Failure(ProductError.InvalidCategoryData, localizer[nameof(ProductError.InvalidCategoryData)]);

        var name = command.Name.Trim();
        if (await categoryRepository.ExistsByNameAsync(command.BusinessId, name, cancellationToken))
            return Result<Category>.Failure(ProductError.DuplicateCategoryName, localizer[nameof(ProductError.DuplicateCategoryName)]);

        var category = new Category(command.BusinessId, name);
        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Category>.Success(category);
    }

    public async Task SeedDefaultCategories(int businessId, CancellationToken cancellationToken)
    {
        foreach (var name in DefaultCategoryNames)
            await categoryRepository.AddAsync(new Category(businessId, name), cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);
    }
}

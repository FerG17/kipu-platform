using FluentValidation;
using Microsoft.Extensions.Localization;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;
using Bodega.Platform.Suppliers.Application.CommandServices;
using Bodega.Platform.Suppliers.Domain.Model.Aggregates;
using Bodega.Platform.Suppliers.Domain.Model.Commands;
using Bodega.Platform.Suppliers.Domain.Model.Errors;
using Bodega.Platform.Suppliers.Domain.Repositories;
using Bodega.Platform.Suppliers.Resources;

namespace Bodega.Platform.Suppliers.Application.Internal.CommandServices;

public class SupplierCommandService(
    ISupplierRepository supplierRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateSupplierCommand> createSupplierValidator,
    IValidator<UpdateSupplierCommand> updateSupplierValidator,
    IStringLocalizer<SuppliersMessages> localizer,
    IBusinessClock businessClock)
    : ISupplierCommandService
{
    public async Task<Result<Supplier>> Handle(CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        if (!(await createSupplierValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Supplier>.Failure(SuppliersError.InvalidSupplierData,
                localizer[nameof(SuppliersError.InvalidSupplierData)]);

        var supplier = new Supplier(command.BusinessId, command.Name, command.LastName, command.Ruc, command.Email,
            command.Phone, command.Address, command.ContactPerson, command.Category, businessClock.Today);
        await supplierRepository.AddAsync(supplier, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Supplier>.Success(supplier);
    }

    public async Task<Result<Supplier>> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        if (!(await updateSupplierValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Supplier>.Failure(SuppliersError.InvalidSupplierData,
                localizer[nameof(SuppliersError.InvalidSupplierData)]);

        var supplier = await supplierRepository.FindByIdAsync(command.SupplierId, cancellationToken);
        if (supplier == null)
            return Result<Supplier>.Failure(SuppliersError.SupplierNotFound, localizer[nameof(SuppliersError.SupplierNotFound)]);

        supplier.UpdateDetails(command.Name, command.LastName, command.Ruc, command.Email, command.Phone, command.Address,
            command.ContactPerson, command.Category);
        supplierRepository.Update(supplier);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Supplier>.Success(supplier);
    }

    public async Task<Result<Supplier>> Handle(DeactivateSupplierCommand command, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.FindByIdAsync(command.SupplierId, cancellationToken);
        if (supplier == null)
            return Result<Supplier>.Failure(SuppliersError.SupplierNotFound, localizer[nameof(SuppliersError.SupplierNotFound)]);

        supplier.Deactivate();
        supplierRepository.Update(supplier);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Supplier>.Success(supplier);
    }
}

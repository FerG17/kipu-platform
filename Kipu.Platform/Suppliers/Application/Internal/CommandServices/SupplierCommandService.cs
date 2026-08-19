using FluentValidation;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;
using Kipu.Platform.Suppliers.Application.CommandServices;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Commands;
using Kipu.Platform.Suppliers.Domain.Model.Errors;
using Kipu.Platform.Suppliers.Domain.Repositories;
using Kipu.Platform.Suppliers.Resources;

namespace Kipu.Platform.Suppliers.Application.Internal.CommandServices;

public class SupplierCommandService(
    ISupplierRepository supplierRepository,
    IPurchaseOrderRepository purchaseOrderRepository,
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

        // The frontend already checks this against its own (possibly stale
        // or not-yet-loaded) list of orders before even offering the confirm
        // dialog — this is the authoritative check, since a supplier still
        // owed a pending or delayed delivery has to stay reachable.
        var orders = await purchaseOrderRepository.FindAllBySupplierIdAsync(command.SupplierId, cancellationToken);
        if (orders.Any(order => order.Status is PurchaseOrderStatus.Pending or PurchaseOrderStatus.Delayed))
            return Result<Supplier>.Failure(SuppliersError.SupplierHasPendingOrders,
                localizer[nameof(SuppliersError.SupplierHasPendingOrders)]);

        supplier.Deactivate();
        supplierRepository.Update(supplier);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Supplier>.Success(supplier);
    }
}

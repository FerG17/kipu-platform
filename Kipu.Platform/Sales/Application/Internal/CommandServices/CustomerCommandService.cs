using FluentValidation;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Sales.Application.CommandServices;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Sales.Domain.Model.Errors;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Sales.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Sales.Application.Internal.CommandServices;

public class CustomerCommandService(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateCustomerCommand> createCustomerValidator,
    IValidator<UpdateCustomerCommand> updateCustomerValidator,
    IStringLocalizer<SalesMessages> localizer)
    : ICustomerCommandService
{
    public async Task<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (!(await createCustomerValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Customer>.Failure(SalesError.InvalidCustomerData,
                localizer[nameof(SalesError.InvalidCustomerData)]);

        var customer = new Customer(command.BusinessId, command.FullName, command.DocumentNumber, command.PhoneNumber,
            command.Email);
        await customerRepository.AddAsync(customer, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Customer>.Success(customer);
    }

    public async Task<Result<Customer>> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (!(await updateCustomerValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Customer>.Failure(SalesError.InvalidCustomerData,
                localizer[nameof(SalesError.InvalidCustomerData)]);

        var customer = await customerRepository.FindByIdAsync(command.CustomerId, cancellationToken);
        if (customer == null)
            return Result<Customer>.Failure(SalesError.CustomerNotFound, localizer[nameof(SalesError.CustomerNotFound)]);

        customer.UpdateDetails(command.FullName, command.DocumentNumber, command.PhoneNumber, command.Email);
        customerRepository.Update(customer);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Customer>.Success(customer);
    }

    public async Task<Result> Handle(DeleteCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.FindByIdAsync(command.CustomerId, cancellationToken);
        if (customer == null)
            return Result.Failure(SalesError.CustomerNotFound, localizer[nameof(SalesError.CustomerNotFound)]);

        customerRepository.Remove(customer);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }
}

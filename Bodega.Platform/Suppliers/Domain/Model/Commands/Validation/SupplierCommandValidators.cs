using FluentValidation;

namespace Bodega.Platform.Suppliers.Domain.Model.Commands.Validation;

/// <summary>Field rules for a supplier — lengths mirror Suppliers' ModelBuilderExtensions.</summary>
public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.LastName).MaximumLength(150);
        RuleFor(command => command.Ruc).MaximumLength(20);
        RuleFor(command => command.Email).MaximumLength(150);
        RuleFor(command => command.Phone).MaximumLength(20);
        RuleFor(command => command.Address).MaximumLength(255);
        RuleFor(command => command.ContactPerson).MaximumLength(150);
        RuleFor(command => command.Category).NotEmpty().MaximumLength(50);
    }
}

/// <summary>Same rules as creation — an edit must not reach a state a create would refuse.</summary>
public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.LastName).MaximumLength(150);
        RuleFor(command => command.Ruc).MaximumLength(20);
        RuleFor(command => command.Email).MaximumLength(150);
        RuleFor(command => command.Phone).MaximumLength(20);
        RuleFor(command => command.Address).MaximumLength(255);
        RuleFor(command => command.ContactPerson).MaximumLength(150);
        RuleFor(command => command.Category).NotEmpty().MaximumLength(50);
    }
}

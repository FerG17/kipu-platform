using FluentValidation;

namespace Bodega.Platform.Products.Domain.Model.Commands.Validation;

/// <summary>Field rules for a new warehouse — lengths mirror Products' ModelBuilderExtensions.</summary>
public class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Code).MaximumLength(30);
        RuleFor(command => command.Address).MaximumLength(255);
        RuleFor(command => command.Capacity).NotEmpty().MaximumLength(20);
    }
}

/// <summary>Same rules as creation — an edit must not reach a state a create would refuse.</summary>
public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Code).MaximumLength(30);
        RuleFor(command => command.Address).MaximumLength(255);
        RuleFor(command => command.Capacity).NotEmpty().MaximumLength(20);
    }
}

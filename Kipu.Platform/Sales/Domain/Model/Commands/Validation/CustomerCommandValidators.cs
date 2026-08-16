using FluentValidation;

namespace Kipu.Platform.Sales.Domain.Model.Commands.Validation;

/// <summary>
///     Field rules for a customer — lengths mirror Sales' ModelBuilderExtensions.
///
///     Customers hold the personal data this app is most accountable for
///     (name, document number, phone), and none of it was checked: a value
///     longer than its column reached MySQL and came back as a 500 instead of
///     a 400, and a blank name was accepted outright.
/// </summary>
public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(150);
        RuleFor(command => command.DocumentNumber).MaximumLength(20);
        RuleFor(command => command.PhoneNumber).MaximumLength(20);
        RuleFor(command => command.Email).MaximumLength(150);
    }
}

/// <summary>Same rules as creation — an edit must not reach a state a create would refuse.</summary>
public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(150);
        RuleFor(command => command.DocumentNumber).MaximumLength(20);
        RuleFor(command => command.PhoneNumber).MaximumLength(20);
        RuleFor(command => command.Email).MaximumLength(150);
    }
}

using FluentValidation;

namespace Kipu.Platform.Dashboard.Domain.Model.Commands.Validation;

public class GenerateReportCommandValidator : AbstractValidator<GenerateReportCommand>
{
    public GenerateReportCommandValidator()
    {
        RuleFor(command => command.Type).NotEmpty();
        RuleFor(command => command)
            .Must(command => !command.DateFrom.HasValue || !command.DateTo.HasValue || command.DateFrom <= command.DateTo)
            .WithMessage("DateFrom must be on or before DateTo.");
    }
}

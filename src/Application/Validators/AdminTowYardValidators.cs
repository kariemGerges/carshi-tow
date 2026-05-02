using CarshiTow.Application.DTOs;
using CarshiTow.Domain.Enums;
using FluentValidation;

namespace CarshiTow.Application.Validators;

public sealed class AdminUpdateTowYardStatusRequestValidator : AbstractValidator<AdminUpdateTowYardStatusRequestDto>
{
    public AdminUpdateTowYardStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is TowYardStatus.Active or TowYardStatus.Suspended or TowYardStatus.Rejected)
            .WithMessage("Status must be active, suspended, or rejected.");

        RuleFor(x => x.Reason)
            .MaximumLength(2000)
            .Must((dto, reason) => RequiresReason(dto.Status) ? !string.IsNullOrWhiteSpace(reason) : true)
            .WithMessage("Reason is required when setting status to suspended or rejected.");
    }

    private static bool RequiresReason(TowYardStatus status) =>
        status is TowYardStatus.Suspended or TowYardStatus.Rejected;
}

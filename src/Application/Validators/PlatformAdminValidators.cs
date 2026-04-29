using CarshiTow.Application.DTOs;
using CarshiTow.Domain.Enums;
using FluentValidation;

namespace CarshiTow.Application.Validators;

public sealed class ChangeUserRoleRequestValidator : AbstractValidator<ChangeUserRoleRequest>
{
    public ChangeUserRoleRequestValidator()
    {
        RuleFor(x => x.Role).NotEmpty()
            .Must(v => Enum.TryParse<UserRole>(v.Trim(), ignoreCase: true, out _))
            .WithMessage("Role must be a valid UserRole.");
    }
}

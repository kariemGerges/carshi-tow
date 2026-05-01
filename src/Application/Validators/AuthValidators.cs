using CarshiTow.Application.Abn;
using CarshiTow.Application.DTOs;
using CarshiTow.Domain.Enums;
using FluentValidation;

namespace CarshiTow.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private static readonly AustralianState[] States = Enum.GetValues<AustralianState>();

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10).MaximumLength(128);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{8,14}$");
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Abn)
            .NotEmpty()
            .Must(abn => AbnChecksum.TryValidate(abn, out _))
            .WithMessage("ABN must contain 11 valid digits that pass checksum validation.");
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Suburb).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).Must(s => States.Contains(s));
        RuleFor(x => x.Postcode).NotEmpty().Length(4).Matches(@"^\d{4}$");
        RuleFor(x => x.BusinessPhone).NotEmpty().Matches(@"^\+?[1-9]\d{8,14}$");
        RuleFor(x => x.VerificationDocumentUrls)
            .Must(urls => urls is null or { Length: <= 10 })
            .WithMessage("At most ten verification document URLs may be supplied.")
            .Must(urls => urls == null || urls.All(u =>
                Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)))
            .When(x => x.VerificationDocumentUrls is { Length: > 0 });
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10).MaximumLength(128);
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.CsrfToken).NotEmpty().MinimumLength(20).MaximumLength(512);
    }
}

public sealed class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches(@"^\d{6}$");
        RuleFor(x => x.Purpose).NotEmpty().Must(v => Enum.TryParse(v, true, out Domain.Enums.OtpPurpose _));
    }
}

public sealed class MfaVerifyRequestValidator : AbstractValidator<MfaVerifyRequest>
{
    public MfaVerifyRequestValidator()
    {
        RuleFor(x => x.MfaAccessToken).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches(@"^\d{6}$");
        RuleFor(x => x.Purpose).NotEmpty().Must(v => Enum.TryParse(v, true, out Domain.Enums.OtpPurpose _));
    }
}

public sealed class RequestPasswordResetRequestValidator : AbstractValidator<RequestPasswordResetRequest>
{
    public RequestPasswordResetRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class CompletePasswordResetRequestValidator : AbstractValidator<CompletePasswordResetRequest>
{
    public CompletePasswordResetRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(10).MaximumLength(128);
    }
}

using CarshiTow.Application.DTOs;
using CarshiTow.Application.PhotoPacks;
using FluentValidation;

namespace CarshiTow.Application.Validators;

public sealed class RequestPhotoUploadSlotRequestValidator : AbstractValidator<RequestPhotoUploadSlotRequest>
{
    public RequestPhotoUploadSlotRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FileSizeBytes)
            .InclusiveBetween(1, PhotoPackRules.MaxPhotoUploadBytes);
    }
}

public sealed class ConfirmPhotoUploadRequestValidator : AbstractValidator<ConfirmPhotoUploadRequest>
{
    public ConfirmPhotoUploadRequestValidator()
    {
        RuleFor(x => x.PhotoId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileSizeBytes)
            .Must(sz => sz >= 1 && (long)sz <= PhotoPackRules.MaxPhotoUploadBytes);
        RuleFor(x => x.WidthPx).GreaterThanOrEqualTo(PhotoPackRules.MinPhotoWidthPx);
        RuleFor(x => x.HeightPx).GreaterThanOrEqualTo(PhotoPackRules.MinPhotoHeightPx);
        RuleFor(x => x.MimeType)
            .Must(PhotoPackRules.IsAcceptedImageMime)
            .WithMessage("Mime type must be JPEG, PNG, or HEIC.")
            .When(x => !string.IsNullOrWhiteSpace(x.MimeType));
    }
}

public sealed class UpdatePhotoInPackRequestValidator : AbstractValidator<UpdatePhotoInPackRequest>
{
    public UpdatePhotoInPackRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.AiCategoryOverride is not null || x.SortOrder is not null)
            .WithMessage("At least one of aiCategoryOverride or sortOrder must be provided.");
        RuleFor(x => x.SortOrder!.Value).GreaterThanOrEqualTo((short)0).When(x => x.SortOrder is not null);
    }
}

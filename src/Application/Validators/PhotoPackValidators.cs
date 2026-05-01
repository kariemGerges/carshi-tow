using CarshiTow.Application.DTOs;
using CarshiTow.Application.PhotoPacks;
using CarshiTow.Domain.Enums;
using FluentValidation;

namespace CarshiTow.Application.Validators;

public sealed class CreatePhotoPackDraftRequestValidator : AbstractValidator<CreatePhotoPackDraftRequest>
{
    public CreatePhotoPackDraftRequestValidator()
    {
        RuleFor(x => x.VehicleRego).NotEmpty().MaximumLength(10).Matches("^[A-Za-z0-9]+$");
        RuleFor(x => x.VehicleMake).NotEmpty().MaximumLength(100);
        RuleFor(x => x.VehicleModel).NotEmpty().MaximumLength(100);
        RuleFor(x => x.VehicleYear)
            .InclusiveBetween((short)1980, (short)(DateTime.UtcNow.Year + 1));
        RuleFor(x => x.VehicleVin).MaximumLength(17).When(x => x.VehicleVin is not null);
        RuleFor(x => x.ClaimReference).MaximumLength(100).When(x => x.ClaimReference is not null);
        RuleFor(x => x.TowYardReference).MaximumLength(100).When(x => x.TowYardReference is not null);
        RuleFor(x => x.TowYardPriceCents)
            .InclusiveBetween(PhotoPackRules.MinTowYardPriceCents, PhotoPackRules.MaxTowYardPriceCents);
    }
}

public sealed class UpdatePhotoPackDraftRequestValidator : AbstractValidator<UpdatePhotoPackDraftRequest>
{
    public UpdatePhotoPackDraftRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.VehicleRego is not null || x.VehicleRegoState is not null || x.VehicleMake is not null
                || x.VehicleModel is not null || x.VehicleYear is not null || x.VehicleVin is not null
                || x.ClaimReference is not null || x.TowYardReference is not null || x.TowYardPriceCents is not null)
            .WithMessage("At least one field must be provided.");

        RuleFor(x => x.VehicleRego!).NotEmpty().MaximumLength(10).Matches("^[A-Za-z0-9]+$")
            .When(x => x.VehicleRego is not null);
        RuleFor(x => x.VehicleMake!).NotEmpty().MaximumLength(100).When(x => x.VehicleMake is not null);
        RuleFor(x => x.VehicleModel!).NotEmpty().MaximumLength(100).When(x => x.VehicleModel is not null);
        RuleFor(x => x.VehicleYear!.Value)
            .InclusiveBetween((short)1980, (short)(DateTime.UtcNow.Year + 1))
            .When(x => x.VehicleYear is not null);
        RuleFor(x => x.VehicleVin!).MaximumLength(17).When(x => x.VehicleVin is not null);
        RuleFor(x => x.ClaimReference!).MaximumLength(100).When(x => x.ClaimReference is not null);
        RuleFor(x => x.TowYardReference!).MaximumLength(100).When(x => x.TowYardReference is not null);
        RuleFor(x => x.TowYardPriceCents!.Value)
            .InclusiveBetween(PhotoPackRules.MinTowYardPriceCents, PhotoPackRules.MaxTowYardPriceCents)
            .When(x => x.TowYardPriceCents is not null);
    }
}

public sealed class PhotoPackListQueryValidator : AbstractValidator<PhotoPackListQuery>
{
    public PhotoPackListQueryValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, PhotoPackRules.MaxListPageSize);
        RuleFor(x => x.MaxTowYardPriceCents)
            .GreaterThanOrEqualTo(x => x.MinTowYardPriceCents)
            .When(x => x is { MinTowYardPriceCents: not null, MaxTowYardPriceCents: not null });
        RuleFor(x => x.CreatedToUtc)
            .GreaterThanOrEqualTo(x => x.CreatedFromUtc)
            .When(x => x is { CreatedFromUtc: not null, CreatedToUtc: not null });
    }
}

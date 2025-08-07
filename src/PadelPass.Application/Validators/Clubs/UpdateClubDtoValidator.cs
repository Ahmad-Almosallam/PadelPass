using FluentValidation;
using PadelPass.Application.DTOs.Clubs;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Validators.Clubs;

public class UpdateClubDtoValidator : AbstractValidator<UpdateClubDto>
{
    private readonly IGlobalLocalizer _localizer;

    public UpdateClubDtoValidator(IGlobalLocalizer localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_localizer["NameRequired"])
            .MaximumLength(200).WithMessage(_localizer["NameMaxLength", 200]);

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage(_localizer["AddressMaxLength"]);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue)
            .WithMessage(_localizer["LatitudeRange"]);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue)
            .WithMessage(_localizer["LongitudeRange"]);
    }
}
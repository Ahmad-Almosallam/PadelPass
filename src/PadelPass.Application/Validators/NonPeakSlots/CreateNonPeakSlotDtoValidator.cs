using FluentValidation;
using PadelPass.Application.DTOs.NonPeakSlots;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Validators.NonPeakSlots
{
    public class CreateNonPeakSlotDtoValidator : AbstractValidator<CreateNonPeakSlotDto>
    {
        private readonly IGlobalLocalizer _localizer;

        public CreateNonPeakSlotDtoValidator(IGlobalLocalizer localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.ClubId)
                .NotEmpty().WithMessage(_localizer["ClubIdRequired"]);

            RuleFor(x => x.DayOfWeek)
                .IsInEnum().WithMessage(_localizer["InvalidDayOfWeek"]);

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage(_localizer["StartTimeRequired"]);

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage(_localizer["EndTimeRequired"])
                .GreaterThan(x => x.StartTime).WithMessage(_localizer["EndTimeAfterStartTime"]);
        }
    }
}
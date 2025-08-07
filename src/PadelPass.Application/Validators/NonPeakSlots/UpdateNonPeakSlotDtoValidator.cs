using FluentValidation;
using PadelPass.Application.DTOs.NonPeakSlots;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Validators.NonPeakSlots
{
    public class UpdateNonPeakSlotDtoValidator : AbstractValidator<UpdateNonPeakSlotDto>
    {
        private readonly IGlobalLocalizer _localizer;

        public UpdateNonPeakSlotDtoValidator(IGlobalLocalizer localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.DayOfWeek)
                .IsInEnum().WithMessage(_localizer["InvalidDayOfWeek"]);

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime).WithMessage(_localizer["EndTimeAfterStartTime"]);
        }
    }
}
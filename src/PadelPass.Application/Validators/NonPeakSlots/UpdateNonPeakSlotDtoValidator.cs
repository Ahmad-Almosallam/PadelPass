using FluentValidation;
using PadelPass.Application.DTOs.NonPeakSlots;

namespace PadelPass.Application.Validators.NonPeakSlots;

public class UpdateNonPeakSlotDtoValidator : AbstractValidator<UpdateNonPeakSlotDto>
{
    public UpdateNonPeakSlotDtoValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("Invalid day of week");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time");
    }
}
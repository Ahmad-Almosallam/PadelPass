using FluentValidation;
using PadelPass.Application.DTOs.NonPeakSlots;

namespace PadelPass.Application.Validators.NonPeakSlots;

public class CreateNonPeakSlotDtoValidator : AbstractValidator<CreateNonPeakSlotDto>
{
    public CreateNonPeakSlotDtoValidator()
    {
        RuleFor(x => x.ClubId)
            .NotEmpty().WithMessage("Club ID is required");

        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("Invalid day of week");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required")
            .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time");
    }
}
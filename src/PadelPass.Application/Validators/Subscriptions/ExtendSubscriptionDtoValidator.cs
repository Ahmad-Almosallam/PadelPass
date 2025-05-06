using FluentValidation;
using PadelPass.Application.DTOs.Subscriptions;

namespace PadelPass.Application.Validators.Subscriptions;

public class ExtendSubscriptionDtoValidator : AbstractValidator<ExtendSubscriptionDto>
{
    public ExtendSubscriptionDtoValidator()
    {
        RuleFor(x => x.AdditionalMonths)
            .NotEmpty().WithMessage("Number of additional months is required")
            .InclusiveBetween(1, 36).WithMessage("Additional months must be between 1 and 36");
    }
}
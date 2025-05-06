using FluentValidation;
using PadelPass.Application.DTOs.Subscriptions;

namespace PadelPass.Application.Validators.Subscriptions;

public class CreateSubscriptionDtoValidator : AbstractValidator<CreateSubscriptionDto>
{
    public CreateSubscriptionDtoValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("Plan ID is required");
    }
}
using FluentValidation;
using PadelPass.Application.DTOs.SubscriptionPlans;

namespace PadelPass.Application.Validators.SubscriptionPlans;

public class CreateSubscriptionPlanDtoValidator : AbstractValidator<CreateSubscriptionPlanDto>
{
    public CreateSubscriptionPlanDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.DurationInMonths)
            .NotEmpty().WithMessage("Duration is required")
            .InclusiveBetween(1, 36).WithMessage("Duration must be between 1 and 36 months");

        RuleFor(x => x.Price)
            .NotEmpty().WithMessage("Price is required")
            .GreaterThan(0).WithMessage("Price must be greater than 0");
    }
}
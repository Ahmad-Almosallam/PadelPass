using FluentValidation;
using PadelPass.Application.DTOs.SubscriptionPlans;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Validators.SubscriptionPlans
{
    public class UpdateSubscriptionPlanDtoValidator : AbstractValidator<UpdateSubscriptionPlanDto>
    {
        private readonly IGlobalLocalizer _localizer;

        public UpdateSubscriptionPlanDtoValidator(IGlobalLocalizer localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(_localizer["NameRequired"])
                .MaximumLength(100).WithMessage(_localizer["NameMaxLength", 100]);

            RuleFor(x => x.DurationInMonths)
                .NotEmpty().WithMessage(_localizer["DurationRequired"])
                .InclusiveBetween(1, 36)
                .WithMessage(_localizer["DurationRange"]);

            RuleFor(x => x.Price)
                .NotEmpty().WithMessage(_localizer["PriceRequired"])
                .GreaterThan(0).WithMessage(_localizer["PriceGreaterThanZero"]);
        }
    }
}
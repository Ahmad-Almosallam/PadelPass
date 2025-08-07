using FluentValidation;
using PadelPass.Application.DTOs.Subscriptions;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Validators.Subscriptions
{
    public class CreateSubscriptionDtoValidator : AbstractValidator<CreateSubscriptionDto>
    {
        private readonly IGlobalLocalizer _localizer;

        public CreateSubscriptionDtoValidator(IGlobalLocalizer localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.PlanId)
                .NotEmpty().WithMessage(_localizer["PlanIdRequired"]);
        }
    }
}
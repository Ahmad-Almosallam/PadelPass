using FluentValidation;
using PadelPass.Application.DTOs.Subscriptions;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Validators.Subscriptions
{
    public class ExtendSubscriptionDtoValidator : AbstractValidator<ExtendSubscriptionDto>
    {
        private readonly IGlobalLocalizer _localizer;

        public ExtendSubscriptionDtoValidator(IGlobalLocalizer localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.AdditionalMonths)
                .NotEmpty().WithMessage(_localizer["AdditionalMonthsRequired"])
                .InclusiveBetween(1, 36)
                .WithMessage(_localizer["AdditionalMonthsRange"]);
        }
    }
}
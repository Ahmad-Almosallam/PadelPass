using FluentValidation;
using PadelPass.Application.DTOs.Subscriptions;

namespace PadelPass.Application.Validators.Subscriptions;

public class UpdateSubscriptionDtoValidator : AbstractValidator<UpdateSubscriptionDto>
{
    public UpdateSubscriptionDtoValidator()
    {
        // Nothing to validate for now
    }
}
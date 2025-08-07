using FluentValidation;
using PadelPass.Application.DTOs.Authentication;
using PadelPass.Core.Services;

namespace PadelPass.Application.Validators.Authentication;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    private readonly IGlobalLocalizer _localizer;

    public LoginDtoValidator(IGlobalLocalizer localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_localizer["EmailRequired"])
            .EmailAddress().WithMessage(_localizer["InvalidEmailFormat"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_localizer["PasswordRequired"]);
    }
}

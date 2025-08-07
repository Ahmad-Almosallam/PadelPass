using FluentValidation;
using PadelPass.Application.DTOs.Authentication;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Validators.Authentication
{
    public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
    {
        private readonly IGlobalLocalizer _localizer;

        public RefreshTokenDtoValidator(IGlobalLocalizer localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage(_localizer["RefreshTokenRequired"]);
        }
    }
}
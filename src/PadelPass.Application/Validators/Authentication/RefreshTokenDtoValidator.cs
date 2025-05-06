using FluentValidation;
using PadelPass.Application.DTOs.Authentication;

namespace PadelPass.Application.Validators.Authentication;

public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
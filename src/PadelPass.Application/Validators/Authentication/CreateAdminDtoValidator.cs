using FluentValidation;
using PadelPass.Application.DTOs.Authentication;
using PadelPass.Core.Services;
using PadelPass.Core.Shared;

namespace PadelPass.Application.Validators.Authentication
{
    public class CreateAdminDtoValidator : AbstractValidator<CreateAdminDto>
    {
        private readonly IGlobalLocalizer _localizer;

        public CreateAdminDtoValidator(IGlobalLocalizer localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_localizer["EmailRequired"])
                .EmailAddress().WithMessage(_localizer["InvalidEmailFormat"]);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(_localizer["PhoneNumberRequired"])
                .MaximumLength(20).WithMessage(_localizer["PhoneNumberMaxLength"]);

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage(_localizer["FullNameRequired"])
                .MaximumLength(100).WithMessage(_localizer["FullNameMaxLength"]);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(_localizer["PasswordRequired"])
                .MinimumLength(6).WithMessage(_localizer["PasswordMinLength"])
                .MaximumLength(50).WithMessage(_localizer["PasswordMaxLength"]);
        }
    }
}
using FluentValidation;
using nizamla.Application.dtos.auth;

namespace nizamla.Application.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı zorunludur");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre zorunludur");
        }
    }
}

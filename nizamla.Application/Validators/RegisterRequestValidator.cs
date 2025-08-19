using FluentValidation;
using nizamla.Application.dtos.auth;

namespace nizamla.Application.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı zorunludur")
                .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalı")
                .MaximumLength(64).WithMessage("Kullanıcı adı en fazla 64 karakter olabilir");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email zorunludur")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre zorunludur")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı")
                .Matches("[A-Z]").WithMessage("Şifre en az 1 büyük harf içermeli")
                .Matches("[a-z]").WithMessage("Şifre en az 1 küçük harf içermeli")
                .Matches("[0-9]").WithMessage("Şifre en az 1 rakam içermeli");
        }
    }
}

using FluentValidation;
using nizamla.Application.dtos.auth;

namespace nizamla.Application.Validators
{
    public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
    {
        public RefreshRequestValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token zorunludur");
        }
    }
}

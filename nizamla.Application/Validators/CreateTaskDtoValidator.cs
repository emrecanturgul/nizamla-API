using FluentValidation;
using nizamla.Application.dtos;

namespace nizamla.Application.Validators
{
    public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
    {
        public CreateTaskDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Başlık zorunludur")
                .MaximumLength(100).WithMessage("Başlık en fazla 100 karakter olabilir");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Açıklama en fazla 1000 karakter olabilir");

            RuleFor(x => x.DueDate)
                .Must(date => date == null || date > DateTime.UtcNow)
                .WithMessage("Bitiş tarihi geçmiş olamaz");
        }
    }
}

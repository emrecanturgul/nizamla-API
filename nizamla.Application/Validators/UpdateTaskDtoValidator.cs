using FluentValidation;
using nizamla.Application.DTOs;

namespace nizamla.Application.Validators
{
    public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
    {
        public UpdateTaskDtoValidator()
        {
            RuleFor(x => x.Title)
                .MaximumLength(100).WithMessage("Başlık en fazla 100 karakter olabilir");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Açıklama en fazla 1000 karakter olabilir");

            RuleFor(x => x.DueDate)
                .Must(date => date == null || date > DateTime.UtcNow)
                .WithMessage("Bitiş tarihi geçmiş olamaz");
        }
    }
}

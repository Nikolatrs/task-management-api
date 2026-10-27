using FluentValidation;
using TaskManagementApi.Models;
using TaskManagementApi.Common;

namespace TaskManagementApi.Validators;
public class PatchTaskRequestValidator : AbstractValidator<PatchTaskRequest>
{
    public PatchTaskRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Title is not null || x.IsCompleted is not null)
            .WithMessage("Не передано ни одного поля для обновления.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название не может быть пустым")
            .MaximumLength(ValidationConstants.MaxTitleLength).WithMessage("Название слишком Длинное")
            .When(x => x.Title is not null);
    }
}

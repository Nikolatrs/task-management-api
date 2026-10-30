using FluentValidation;
using TaskManagementApi.Models;
using TaskManagementApi.Common;

namespace TaskManagementApi.Validators;
public class CreateTaskRequestValidator : AbstractValidator<PostTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название не может быть пустым")
            .MaximumLength(ValidationConstants.MaxTitleLength).WithMessage("Название слишком Длинное");
    }
}

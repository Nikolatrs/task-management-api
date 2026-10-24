using FluentValidation;
using TaskManagementApi.Models;
using TaskManagementApi.Common;

namespace TaskManagementApi.Validators;
public class UpdateTaskRequestValidator : AbstractValidator<PutTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название не может быть пустым")
            .MaximumLength(ValidationConstants.MaxBookTitleLength).WithMessage("Название слишком Длинное");
    }
}

using FluentValidation;
using HelpDisk.Application.Features.Categories.Dtos;
using HelpDisk.Domain.Categories;

namespace HelpDisk.Application.Features.Categories.Validators;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(Category.NameMaxLength);

        RuleFor(x => x.ResponseTimeTargetHours)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Response time target cannot be negative.");
    }
}

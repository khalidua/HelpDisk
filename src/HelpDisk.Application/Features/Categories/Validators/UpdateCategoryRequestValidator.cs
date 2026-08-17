using FluentValidation;

using HelpDisk.Application.Features.Categories.Dtos;
using HelpDisk.Domain.Categories;

public sealed class UpdateCategoryRequestValidator
    : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(Category.NameMaxLength);

        RuleFor(x => x.ResponseTimeTargetHours)
            .GreaterThan(0)
            .WithMessage("Response time target must be at least 1 hour.");
    }
}
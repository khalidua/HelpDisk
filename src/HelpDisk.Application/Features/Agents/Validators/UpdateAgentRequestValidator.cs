using FluentValidation;

using HelpDisk.Application.Features.Agents.Dtos;

namespace HelpDisk.Application.Features.Agents.Validators;

public sealed class UpdateAgentRequestValidator
    : AbstractValidator<UpdateAgentRequest>
{
    public UpdateAgentRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
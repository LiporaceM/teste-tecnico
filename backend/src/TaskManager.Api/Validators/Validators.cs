using FluentValidation;
using TaskManager.Api.Dtos;

namespace TaskManager.Api.Validators;

public class TaskRequestValidator : AbstractValidator<TaskRequest>
{
    public TaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(150).WithMessage("O título deve ter no máximo 150 caracteres.");
        RuleFor(x => x.Description).MaximumLength(2000).WithMessage("A descrição deve ter no máximo 2000 caracteres.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status inválido.");
        RuleFor(x => x.Priority).IsInEnum().WithMessage("Prioridade inválida.");
    }
}

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator() =>
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status inválido.");
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("O nome é obrigatório.").MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().WithMessage("O e-mail é obrigatório.").EmailAddress().WithMessage("E-mail inválido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(6).WithMessage("A senha deve ter ao menos 6 caracteres.");
    }
}

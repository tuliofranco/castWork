using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Application.Projetos.Commands;

using FluentValidation;

namespace Castwork.Application.Ferramentas.Modelo3d.Commands.UnirCorposSobrepostos;

public sealed class UnirCorposSobrepostosCommandValidator : ValidatorBase<UnirCorposSobrepostosCommand>
{
    public UnirCorposSobrepostosCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.ArquivoId).GreaterThan(0).WithName("Arquivo");

        RuleFor(c => c.Corpos)
            .NotNull()
            .Must(c => c.Count >= 2).WithMessage("Escolha ao menos dois corpos para unir.")
            .Must(c => c.Count <= UnirCorposSobrepostosCommand.CorposMaximo).WithMessage($"Informe no máximo {UnirCorposSobrepostosCommand.CorposMaximo} corpos.")
            .Must(c => c.Distinct(StringComparer.Ordinal).Count() == c.Count).WithMessage("Cada corpo só pode aparecer uma vez.")
            .Must(c => c.All(nome => !string.IsNullOrWhiteSpace(nome))).WithMessage("Informe o nome de cada corpo.")
            .WithName("Corpos");
    }
}

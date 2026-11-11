using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Application.Projetos.Commands;

using FluentValidation;

namespace Castwork.Application.Ferramentas.Modelo3d.Commands.DefinirMaterialDosCorpos;

public sealed class DefinirMaterialDosCorposCommandValidator : ValidatorBase<DefinirMaterialDosCorposCommand>
{
    public DefinirMaterialDosCorposCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.ArquivoId).GreaterThan(0).WithName("Arquivo");

        RuleFor(c => c.Corpos)
            .NotNull()
            .Must(c => c.Count > 0).WithMessage("Escolha o material de ao menos um corpo.")
            .Must(c => c.Count <= DefinirMaterialDosCorposCommand.CorposMaximo).WithMessage($"Informe no máximo {DefinirMaterialDosCorposCommand.CorposMaximo} corpos.")
            .Must(c => c.Select(x => x.Corpo).Distinct(StringComparer.Ordinal).Count() == c.Count).WithMessage("Cada corpo só pode aparecer uma vez.")
            .WithName("Corpos");

        RuleForEach(c => c.Corpos).ChildRules(corpo =>
        {
            corpo.RuleFor(x => x.Corpo).NotEmpty().WithName("Corpo");
            corpo.RuleFor(x => x.Material).NotEmpty().WithName("Material");
        });

        // Só os materiais do ateliê: liga ativa, "Pedra" ou "Apoio (não conta)". Tirar o material não é opção.
        RuleFor(c => c.Corpos)
            .MustAsync(async (corpos, ct) =>
            {
                var materiais = (await MateriaisDoModelo.CarregarAsync(UnitOfWork, ct)).Select(m => m.Nome).ToHashSet(StringComparer.Ordinal);
                return corpos.All(c => string.IsNullOrEmpty(c.Material) || materiais.Contains(c.Material));
            })
            .WithMessage("Escolha um material do ateliê: uma liga ativa, Pedra ou Apoio (não conta).")
            .WithName("Material")
            .When(c => c.Corpos is { Count: > 0 });
    }
}

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.DefinirMetaisDaVersao;

public sealed class DefinirMetaisDaVersaoCommandValidator : ValidatorBase<DefinirMetaisDaVersaoCommand>
{
    public DefinirMetaisDaVersaoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);

        RuleFor(c => c.Metais)
            .NotNull()
            .Must(m => m.Count <= VersaoProjeto.MetaisMaximo).WithMessage($"Informe no máximo {VersaoProjeto.MetaisMaximo} ligas.")
            .Must(m => m.Select(x => x.LigaId).Distinct().Count() == m.Count).WithMessage("Cada liga só pode aparecer uma vez.")
            .WithName("Metais");

        // Só liga ativa entra: a desativada que já estava na versão é pendência e precisa ser trocada.
        RuleForEach(c => c.Metais).ChildRules(metal =>
        {
            metal.RuleFor(x => x.LigaId)
                .GreaterThan(0).WithMessage("Informe a liga.")
                .MustExists<MetalInformado, Liga>(UnitOfWork).WithMessage("Liga não encontrada ou desativada.")
                .WithName("Liga");
            metal.RuleFor(x => x.PesoGramas)
                .GreaterThan(0)
                .LessThanOrEqualTo(MetalDaVersao.PesoMaximoGramas)
                .PrecisionScale(10, 3, ignoreTrailingZeros: true)
                .WithName("Peso");
        });
    }
}

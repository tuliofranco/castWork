using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Application.Precificacao.Commands;
using Castwork.Application.Precificacao.Commands.AtualizarCotacao;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.AlterarPrecosDoProjeto;

public sealed class AlterarPrecosDoProjetoCommandValidator : ValidatorBase<AlterarPrecosDoProjetoCommand>
{
    public AlterarPrecosDoProjetoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        this.ValidarParametrosDePreco();

        RuleFor(c => c.Cotacoes)
            .NotNull()
            .Must(c => c.Select(x => x.Metal).Distinct().Count() == c.Count).WithMessage("Informe uma cotação por metal.")
            .WithName("Cotações");

        // Mesmas faixas da cotação do ateliê: R$/g do metal puro, positiva.
        RuleForEach(c => c.Cotacoes).ChildRules(cotacao =>
        {
            cotacao.RuleFor(x => x.Metal).IsInEnum().WithName("Metal");
            cotacao.RuleFor(x => x.Valor)
                .GreaterThan(0)
                .LessThanOrEqualTo(AtualizarCotacaoCommandValidator.ValorMaximo)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .WithName("Cotação");
        });
    }
}

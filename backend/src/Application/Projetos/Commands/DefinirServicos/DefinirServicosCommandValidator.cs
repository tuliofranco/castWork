using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.DefinirServicos;

public sealed class DefinirServicosCommandValidator : ValidatorBase<DefinirServicosCommand>
{
    public DefinirServicosCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);

        RuleFor(c => c.Servicos)
            .NotNull()
            .Must(s => s.Count <= VersaoProjeto.ServicosMaximo).WithMessage($"Informe no máximo {VersaoProjeto.ServicosMaximo} serviços.")
            .WithName("Serviços");

        RuleForEach(c => c.Servicos).ChildRules(servico =>
        {
            servico.RuleFor(x => x.Nome).NotEmpty().MaximumLength(ServicoExterno.NomeMaxLength).WithName("Serviço");
            servico.RuleFor(x => x.Valor)
                .GreaterThan(0)
                .LessThanOrEqualTo(ServicoExterno.ValorMaximo)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .WithName("Valor");
        });
    }
}

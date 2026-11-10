using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;

/// <summary>
/// Padrões de preço do ateliê: perda de fundição e acabamento, valor da hora, markup, taxas e arredondamento.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class AtualizarParametrosDePrecoCommand : IRequest
{
    /// <summary>
    /// % (8 = 8%).
    /// </summary>
    public decimal PerdaPercentual { get; set; }

    public decimal ValorHora { get; set; }

    /// <summary>
    /// Fator sobre o custo (1,8 = custo × 1,8).
    /// </summary>
    public decimal Markup { get; set; }

    /// <summary>
    /// % de cartão e impostos.
    /// </summary>
    public decimal TaxasPercentual { get; set; }

    /// <summary>
    /// Múltiplo em R$ para arredondar o preço para cima. Zero não arredonda.
    /// </summary>
    public decimal ArredondarPara { get; set; }
}

public sealed class AtualizarParametrosDePrecoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<AtualizarParametrosDePrecoCommand>
{
    public async Task Handle(AtualizarParametrosDePrecoCommand request, CancellationToken cancellationToken)
    {
        var configuracao = await unitOfWork.GetRepository<ConfiguracaoAtelie>()
            .ObterPorIdOuFalharAsync(ConfiguracaoAtelie.IdUnico, cancellationToken);

        configuracao.AtualizarParametrosDePreco(ParametrosDePreco.Criar(
            request.PerdaPercentual,
            Dinheiro.Criar(request.ValorHora),
            request.Markup,
            request.TaxasPercentual,
            Dinheiro.Criar(request.ArredondarPara)));

        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

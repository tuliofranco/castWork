using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Precificacao.Commands;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Projetos.Commands.AlterarPrecosDoProjeto;

/// <summary>
/// Edita os preços só deste projeto: as cotações informadas substituem as do projeto (metal ausente fica sem cotação)
/// e os parâmetros são trocados. O ateliê e os outros projetos não mudam.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class AlterarPrecosDoProjetoCommand : ComandoDeProjeto, IParametrosDePrecoInformados, IRequest
{
    public IReadOnlyList<CotacaoInformada> Cotacoes { get; set; } = [];

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

/// <summary>
/// R$/g do metal puro no projeto.
/// </summary>
public sealed class CotacaoInformada
{
    public Metal Metal { get; set; }

    public decimal Valor { get; set; }
}

public sealed class AlterarPrecosDoProjetoCommandHandler(IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<AlterarPrecosDoProjetoCommand>
{
    public async Task Handle(AlterarPrecosDoProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);

        projeto.AlterarPrecos(
            request.Cotacoes.ToDictionary(c => c.Metal, c => Dinheiro.Criar(c.Valor)),
            request.ParaParametrosDePreco(),
            timeProvider.GetUtcNow());

        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

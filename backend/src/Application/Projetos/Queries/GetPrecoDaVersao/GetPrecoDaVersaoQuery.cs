using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.DomainService.Precificacao;

using MediatR;

namespace Castwork.Application.Projetos.Queries.GetPrecoDaVersao;

/// <summary>
/// O preço da versão, calculado agora (nada é gravado) pela <see cref="CalculadoraDePreco"/> com os preços do projeto.
/// Quem não tem <c>precificacao.ler</c> recebe só o preço final e o status.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetPrecoDaVersaoQuery : IRequest<PrecoDaVersaoDto>
{
    public long VersaoId { get; set; }
}

public sealed class GetPrecoDaVersaoQueryHandler(
    IUnitOfWork unitOfWork,
    ISessaoService sessao,
    IPermissaoService permissaoService,
    TimeProvider timeProvider) : IRequestHandler<GetPrecoDaVersaoQuery, PrecoDaVersaoDto>
{
    public async Task<PrecoDaVersaoDto> Handle(GetPrecoDaVersaoQuery request, CancellationToken cancellationToken)
    {
        var entrada = await MontarEntradaAsync(request.VersaoId, cancellationToken);
        var preco = CalculadoraDePreco.Calcular(entrada);
        var podeVerCalculo = await permissaoService.SessaoPossuiRegraAsync(sessao, Regras.Precificacao.Consultar, cancellationToken);

        return new PrecoDaVersaoDto
        {
            VersaoId = request.VersaoId,
            Preco = preco.Preco.Valor,
            Status = preco.Status,
            Calculo = podeVerCalculo ? ParaDto(preco, entrada.MarkupDaVersao) : null,
        };
    }

    private static CalculoDoPrecoDto ParaDto(PrecoCalculado preco, decimal? markupDaVersao)
        => new()
        {
            Metais = [.. preco.Metais.Select(m => new CustoDoMetalDto
            {
                LigaId = m.LigaId,
                Liga = m.Liga,
                Metal = m.Metal,
                Teor = m.Teor.Milesimos,
                LigaAtiva = m.LigaAtiva,
                PesoEstimado = m.PesoEstimado.Gramas,
                PesoAFundir = m.PesoAFundir.Gramas,
                CotacaoDoPuro = m.CotacaoDoPuro?.Valor,
                ValorPorGrama = m.ValorPorGrama?.Valor,
                Custo = m.Custo.Valor,
                PesoReal = m.PesoReal?.Gramas,
                DiferencaDoPesoReal = m.DiferencaDoPesoReal,
            })],
            CustoDosMetais = preco.CustoDosMetais.Valor,
            Composicao = [.. preco.Composicao.Select(p => new CustoDaPecaDto
            {
                PecaId = p.PecaId,
                Nome = p.Nome,
                Quantidade = p.Quantidade,
                CustoUnitario = p.CustoUnitario?.Valor,
                Custo = p.Custo.Valor,
            })],
            CustoDaComposicao = preco.CustoDaComposicao.Valor,
            MaoDeObra = new CustoDaMaoDeObraDto
            {
                Tipo = preco.MaoDeObra.Tipo,
                Horas = preco.MaoDeObra.Horas,
                ValorHora = preco.MaoDeObra.ValorHora?.Valor,
                Custo = preco.MaoDeObra.Custo.Valor,
            },
            Servicos = [.. preco.Servicos.Select(s => new ServicoExternoDto { Nome = s.Nome, Valor = s.Valor.Valor })],
            CustoDosServicos = preco.CustoDosServicos.Valor,
            CustoTotal = preco.CustoTotal.Valor,
            PerdaPercentual = preco.PerdaPercentual,
            Markup = preco.Markup,
            MarkupDaVersao = markupDaVersao,
            TaxasPercentual = preco.TaxasPercentual,
            ArredondarPara = preco.ArredondarPara.Valor,
            PrecoSemArredondar = preco.PrecoSemArredondar.Valor,
            Pendencias = [.. preco.Pendencias.Select(p => new PendenciaDoPrecoDto
            {
                Tipo = p.Tipo,
                Descricao = p.Descricao,
                PecaId = p.PecaId,
                LigaId = p.LigaId,
                Metal = p.Metal,
            })],
            Avisos = [.. preco.Avisos.Select(a => new AvisoDoPrecoDto { Tipo = a.Tipo, Descricao = a.Descricao, Metal = a.Metal })],
        };

    /// <summary>
    /// Lê a versão com os preços do projeto, as ligas usadas e o custo das peças da composição.
    /// </summary>
    private async Task<EntradaDoPreco> MontarEntradaAsync(long versaoId, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Versoes.Any(v => v.Id == versaoId))
            .Include(p => p.Cotacoes)
            .Include(p => p.Versoes.Where(v => v.Id == versaoId)).ThenInclude(v => v.Itens)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Versão", versaoId);
        var versao = projeto.Versoes.Single();

        var ligaIds = versao.Metais.Select(m => m.LigaId).ToList();
        var ligas = await unitOfWork.GetRepository<Liga>()
            .FindBy(l => ligaIds.Contains(l.Id))
            .AsNoTracking()
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var pecaIds = versao.Itens.Select(i => i.PecaId).ToList();
        var pecas = await unitOfWork.GetRepository<Peca>()
            .FindBy(p => pecaIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nome, p.Custo })
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return new EntradaDoPreco(
            projeto.ParametrosDePreco,
            [.. projeto.Cotacoes.Select(c => new CotacaoUsada(c.Metal, c.Valor, c.CotadaEm))],
            [.. versao.Metais.Select(m => ligas.TryGetValue(m.LigaId, out var liga)
                ? new MetalUsado(m.LigaId, liga.Nome, liga.Metal, liga.Teor, liga.Ativo, m.PesoEstimado, m.PesoReal)
                : new MetalUsado(m.LigaId, $"Liga {m.LigaId}", Metal.Ouro, Domain.ValueObjects.TeorMetal.Ouro24k, LigaAtiva: false, m.PesoEstimado, m.PesoReal))],
            [.. versao.Itens.OrderBy(i => i.Id).Select(i => new PecaUsada(i.PecaId, pecas[i.PecaId].Nome, i.Quantidade, pecas[i.PecaId].Custo))],
            versao.MaoDeObra,
            versao.Servicos,
            versao.Markup,
            timeProvider.GetUtcNow());
    }
}

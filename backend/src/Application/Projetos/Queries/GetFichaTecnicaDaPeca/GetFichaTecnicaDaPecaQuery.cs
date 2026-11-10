using System.Globalization;

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Ferramentas.Briefings;
using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Application.Projetos.Queries.GetPesoDaPeca;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Ferramentas.Briefings;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Projetos.Queries.GetFichaTecnicaDaPeca;

/// <summary>
/// A ficha técnica da peça do projeto em PDF, gerada na hora (nada é guardado): dados da peça, peso com a origem
/// (<see cref="GetPesoDaPecaQuery"/>), descrição, projeto, resumo do briefing e data de emissão. O valor no projeto só
/// entra para quem tem <c>precificacao.ler</c>. Com as imagens guardadas do desenho da peça (a iso do modelo 3D e a do
/// desenho 2D, em PNG), quando o arquivo final já as tem; a prévia sob demanda não entra.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetFichaTecnicaDaPecaQuery : IRequest<byte[]>
{
    public long ProjetoId { get; set; }

    public long PecaDoProjetoId { get; set; }
}

public sealed class GetFichaTecnicaDaPecaQueryHandler(
    IUnitOfWork unitOfWork,
    ISessaoService sessao,
    IPermissaoService permissaoService,
    IMedidorDoModelo medidor,
    IGeradorFichaTecnica gerador,
    TimeProvider relogio) : IRequestHandler<GetFichaTecnicaDaPecaQuery, byte[]>
{
    /// <summary>
    /// Tamanho máximo do resumo do briefing na ficha.
    /// </summary>
    private const int TamanhoDoResumo = 400;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<byte[]> Handle(GetFichaTecnicaDaPecaQuery request, CancellationToken cancellationToken)
    {
        var peca = await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Id == request.ProjetoId)
            .SelectMany(p => p.Pecas)
            .Where(p => p.Id == request.PecaDoProjetoId)
            .Select(p => new { p.Nome, p.Tipo, p.Status, p.Material, p.Teor, p.Descricao, p.Valor })
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Peça do projeto", request.PecaDoProjetoId);

        var peso = await new GetPesoDaPecaQueryHandler(unitOfWork, medidor)
            .Handle(new GetPesoDaPecaQuery { PecaDoProjetoId = request.PecaDoProjetoId }, cancellationToken);
        var podeVerValor = await permissaoService.SessaoPossuiRegraAsync(sessao, Regras.Precificacao.Consultar, cancellationToken);
        var (projeto, cliente) = await ContextoDoBriefing.ProjetoEClienteAsync(unitOfWork, request.ProjetoId, cancellationToken);
        var briefing = await unitOfWork.GetRepository<Briefing>()
            .FindBy(b => b.ProjetoId == request.ProjetoId)
            .Select(b => b.OQueSeraFeito ?? b.OQueOClienteQuer)
            .SingleOrDefaultAsync(cancellationToken);

        var especificacoes = new List<(string, string)>
        {
            ("Tipo", peca.Tipo.ToString()),
            ("Status", peca.Status == StatusPecaDoProjeto.Publicada ? "Publicada na biblioteca" : "Rascunho"),
        };
        if (!string.IsNullOrWhiteSpace(peca.Material))
        {
            especificacoes.Add((peca.Tipo == TipoPeca.Pedra ? "Material" : "Liga", peca.Material));
        }

        if (peca.Teor is not null)
        {
            especificacoes.Add(("Teor", $"{peca.Teor.Milesimos} ({peca.Teor.Quilates.ToString("0.#", PtBr)}k)"));
        }

        especificacoes.Add(("Peso", Peso(peso)));

        var dados = new DadosFichaTecnica(
            await ContextoDoBriefing.MarcaAsync(unitOfWork, cancellationToken),
            peca.Nome,
            projeto,
            cliente,
            DateOnly.FromDateTime(relogio.GetLocalNow().DateTime),
            especificacoes,
            string.IsNullOrWhiteSpace(peca.Descricao) ? null : peca.Descricao,
            podeVerValor ? peca.Valor?.ToString() ?? "Sem valor" : null,
            Resumo(briefing),
            await ImagensAsync(request.PecaDoProjetoId, cancellationToken));
        return await gerador.GerarPdfAsync(dados, cancellationToken);
    }

    private static string Peso(PesoDaPecaDto peso)
    {
        var gramas = peso.Gramas is { } g ? $"{g.ToString("0.###", PtBr)} g" : "Sem peso";
        return peso.Origem switch
        {
            OrigemDoPesoDaPeca.Modelo => $"{gramas} (medido no modelo 3D)",
            OrigemDoPesoDaPeca.ModeloNaoMedido => "Sem peso (o modelo 3D ainda não pôde ser medido)",
            _ when peso.Gramas is null => gramas,
            _ => $"{gramas} (digitado)",
        };
    }

    private static string? Resumo(string? briefing)
    {
        if (string.IsNullOrWhiteSpace(briefing))
        {
            return null;
        }

        var texto = briefing.Trim();
        return texto.Length <= TamanhoDoResumo ? texto : string.Concat(texto.AsSpan(0, TamanhoDoResumo).TrimEnd(), "…");
    }

    /// <summary>
    /// A iso do modelo 3D e o desenho 2D em PNG, do arquivo final de cada ferramenta da peça.
    /// </summary>
    private async Task<IReadOnlyList<ImagemDaFicha>> ImagensAsync(long pecaDoProjetoId, CancellationToken cancellationToken)
    {
        var legendas = new Dictionary<string, string> { [VistaDaImagem.Iso] = "Modelo 3D", [VistaDaImagem.Desenho2d] = "Desenho 2D" };
        var imagens = new List<(string Vista, byte[] Png)>();
        foreach (var desenho in await DesenhosDaPecaDoProjeto.FinaisAsync(unitOfWork, pecaDoProjetoId, cancellationToken))
        {
            imagens.AddRange(desenho.Imagens
                .Where(i => legendas.ContainsKey(i.Vista) && TipoDaImagemGuardada.Detectar(i.Conteudo) == TipoDaImagemGuardada.Png)
                .Select(i => (i.Vista, i.Conteudo)));
        }

        return [.. imagens.OrderBy(i => VistaDaImagem.Ordem(i.Vista)).Select(i => new ImagemDaFicha(legendas[i.Vista], i.Png))];
    }
}

using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Entities.Briefings;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// IA de mentira, determinística, para os testes não chamarem a API real (custo, rede, respostas variáveis).
/// "falhe" simula indisponibilidade; uma fala com "pdf" pede o documento; qualquer outra vira o
/// "o que será feito" do briefing. Só imagens, sem texto, recebem "Vi a imagem.".
/// </summary>
public sealed class AssistenteBriefingDeTeste : IAssistenteBriefing
{
    /// <summary>
    /// Imagens recebidas no último turno (todas as da conversa), para os testes conferirem o que iria à IA.
    /// </summary>
    public static IReadOnlyDictionary<Guid, ImagemDaConversa> UltimasImagens { get; private set; } = new Dictionary<Guid, ImagemDaConversa>();

    public async Task<IReadOnlyList<MensagemDaConversa>> ConversarAsync(
        string contexto,
        IReadOnlyList<MensagemDaConversa> historico,
        string? falaDoJoalheiro,
        IReadOnlyList<Guid> imagensDaFala,
        IReadOnlyDictionary<Guid, ImagemDaConversa> imagens,
        IFerramentasDoBriefing ferramentas,
        CancellationToken cancellationToken)
    {
        UltimasImagens = imagens;
        if (falaDoJoalheiro == "falhe")
        {
            throw new ServicoIndisponivelException("A IA não respondeu agora. Tente de novo em instantes.");
        }

        var novas = new List<MensagemDaConversa> { new(PapelMensagem.Joalheiro, falaDoJoalheiro, Conteudo(falaDoJoalheiro ?? "abertura")) };
        string resposta;
        if (falaDoJoalheiro is null && imagensDaFala.Count > 0)
        {
            resposta = "Vi a imagem.";
        }
        else if (falaDoJoalheiro is null)
        {
            resposta = "Olá! Qual a medida?";
        }
        else if (falaDoJoalheiro.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            novas.Add(new(PapelMensagem.Ferramenta, null, Conteudo(await ferramentas.GerarPdfAsync(cancellationToken))));
            resposta = "PDF pronto.";
        }
        else
        {
            novas.Add(new(PapelMensagem.Ferramenta, null, Conteudo(ferramentas.Refinar(new RefinamentoBriefing(OQueSeraFeito: falaDoJoalheiro)))));
            resposta = "Anotado.";
        }

        novas.Add(new(PapelMensagem.Assistente, resposta, Conteudo(resposta)));
        return novas;
    }

    private static string Conteudo(string texto) => JsonSerializer.Serialize(new { texto });
}

/// <summary>
/// Gerador de PDF de mentira: devolve bytes fixos e guarda os últimos dados recebidos para conferência.
/// </summary>
public sealed class GeradorDocumentoBriefingDeTeste : IGeradorDocumentoBriefing
{
    public static readonly byte[] Pdf = "%PDF-1.7 teste"u8.ToArray();

    public static DadosDocumentoBriefing? UltimosDados { get; private set; }

    public Task<byte[]> GerarPdfAsync(DadosDocumentoBriefing dados, CancellationToken cancellationToken)
    {
        UltimosDados = dados;
        return Task.FromResult(Pdf);
    }
}

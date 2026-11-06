using System.Globalization;
using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Ferramentas.Briefings;
using Castwork.Domain.Exceptions;

namespace Castwork.Application.Ferramentas.Briefings;

/// <summary>
/// O que a IA do briefing pode fazer no sistema: gravar os campos que o joalheiro confirmou e gerar o PDF.
/// Alteram o briefing já carregado; o command salva tudo no fim do turno.
/// </summary>
internal sealed class FerramentasDoBriefing(IUnitOfWork unitOfWork, IGeradorDocumentoBriefing gerador, TimeProvider relogio, Briefing briefing)
    : IFerramentasLocais
{
    public const string AtualizarBriefing = "atualizar_briefing";
    public const string GerarPdf = "gerar_pdf";

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly string[] TextoOuNulo = ["string", "null"];

    private static readonly string[] CamposDoRefinamento =
        ["oQueSeraFeito", "materiais", "medidas", "acabamento", "pedras", "fecho", "prazo", "orcamento", "observacoes"];

    private static readonly IReadOnlyList<FerramentaLocal> Todas =
    [
        new(
            AtualizarBriefing,
            "Grava no briefing os campos que o joalheiro confirmou. Campos null não são alterados. Prazo no formato dd/MM/aaaa.",
            Esquema(CamposDoRefinamento),
            "Atualizando o briefing"),
        new(
            GerarPdf,
            "Gera o PDF do briefing com os dados gravados e o guarda no projeto para download.",
            Esquema([]),
            "Gerando o PDF"),
    ];

    /// <summary>
    /// Um PDF foi gerado neste turno (a tela atualiza a lista de documentos).
    /// </summary>
    public bool GerouPdf { get; private set; }

    public IReadOnlyList<FerramentaLocal> Definicoes => Todas;

    public async Task<ResultadoLocal> ExecutarAsync(string nome, IReadOnlyDictionary<string, JsonElement> entrada, CancellationToken cancellationToken)
    {
        switch (nome)
        {
            case AtualizarBriefing:
                DateOnly? prazo = null;
                if (Texto(entrada, "prazo") is { } prazoTexto)
                {
                    if (!DateOnly.TryParseExact(prazoTexto, "dd/MM/yyyy", PtBr, DateTimeStyles.None, out var data))
                    {
                        return new ResultadoLocal($"Prazo inválido: use o formato dd/MM/aaaa (recebido: {prazoTexto}).", true);
                    }

                    prazo = data;
                }

                try
                {
                    briefing.Refinar(new RefinamentoBriefing(
                        OQueSeraFeito: Texto(entrada, "oQueSeraFeito"),
                        Materiais: Texto(entrada, "materiais"),
                        Medidas: Texto(entrada, "medidas"),
                        Acabamento: Texto(entrada, "acabamento"),
                        Pedras: Texto(entrada, "pedras"),
                        Fecho: Texto(entrada, "fecho"),
                        Prazo: prazo,
                        Orcamento: Texto(entrada, "orcamento"),
                        Observacoes: Texto(entrada, "observacoes")));
                    return new ResultadoLocal("Briefing atualizado.");
                }
                catch (ValorInvalidoException ex)
                {
                    // Volta para a IA corrigir, em vez de derrubar a conversa.
                    return new ResultadoLocal($"Não foi possível atualizar: {ex.Message}", true);
                }

            case GerarPdf:
                try
                {
                    var documento = await GeracaoDePdf.GerarAsync(unitOfWork, gerador, relogio, briefing, cancellationToken);
                    GerouPdf = true;
                    return new ResultadoLocal($"PDF gerado ({documento.NomeArquivo}). Ele aparece na lista de documentos do briefing para baixar.");
                }
                catch (Common.Exceptions.ServicoIndisponivelException ex)
                {
                    return new ResultadoLocal($"Não foi possível gerar o PDF: {ex.Message}", true);
                }

            default:
                return new ResultadoLocal($"Ferramenta desconhecida: {nome}.", true);
        }
    }

    /// <summary>
    /// Modo estrito: todo campo é obrigatório e aceita null (null = não alterar).
    /// </summary>
    private static string Esquema(string[] campos) => JsonSerializer.Serialize(new Dictionary<string, object>
    {
        ["type"] = "object",
        ["properties"] = campos.ToDictionary(c => c, _ => (object)new { type = TextoOuNulo }),
        ["required"] = campos,
        ["additionalProperties"] = false,
    });

    private static string? Texto(IReadOnlyDictionary<string, JsonElement> entrada, string campo)
        => entrada.TryGetValue(campo, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;
}

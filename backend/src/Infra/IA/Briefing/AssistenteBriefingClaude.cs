using System.Globalization;
using System.Text.Json;

using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Entities.Briefings;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.IA.Briefing;

/// <summary>
/// Conduz a conversa do briefing com a Claude. Um turno pode ter várias chamadas: enquanto a IA pedir
/// ferramentas, elas são executadas e o resultado volta para ela, até a resposta final ao joalheiro.
/// </summary>
internal sealed partial class AssistenteBriefingClaude(IOptions<AnthropicOptions> options, ILogger<AssistenteBriefingClaude> logger)
    : IAssistenteBriefing
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly string[] CamposDoRefinamento =
        ["oQueSeraFeito", "materiais", "medidas", "acabamento", "pedras", "fecho", "prazo", "orcamento", "observacoes"];

    private readonly AnthropicOptions _opcoes = options.Value;

    public async Task<IReadOnlyList<MensagemDaConversa>> ConversarAsync(
        string contexto,
        IReadOnlyList<MensagemDaConversa> historico,
        string? falaDoJoalheiro,
        IFerramentasDoBriefing ferramentas,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.ApiKey))
        {
            throw new ServicoIndisponivelException("A IA do briefing não está configurada neste ateliê.");
        }

        var cliente = new AnthropicClient { ApiKey = _opcoes.ApiKey };
        var novas = new List<MensagemDaConversa>
        {
            // Sem fala (abertura), a mensagem do joalheiro fica oculta no chat: só a IA a vê.
            Mensagem(PapelMensagem.Joalheiro, falaDoJoalheiro, [new BlocoGuardado("texto", falaDoJoalheiro ?? Instrucoes.AberturaOculta)]),
        };

        for (var chamada = 0; chamada < _opcoes.MaximoDeChamadasPorTurno; chamada++)
        {
            var resposta = await ChamarAsync(cliente, contexto, [.. historico, .. novas], cancellationToken);
            var blocos = Guardar(resposta);
            var texto = string.Join("\n\n", blocos.Where(b => b.Tipo == "texto").Select(b => b.Texto));
            var pedidos = blocos.Where(b => b.Tipo == "ferramenta").ToList();

            if (pedidos.Count == 0 && string.IsNullOrWhiteSpace(texto))
            {
                // Recusa ou resposta vazia: o joalheiro precisa de um retorno.
                texto = "Não consegui responder a isso. Pode reformular a mensagem?";
                blocos = [.. blocos, new BlocoGuardado("texto", texto)];
            }

            novas.Add(Mensagem(PapelMensagem.Assistente, texto, blocos));
            if (pedidos.Count == 0)
            {
                return novas;
            }

            var resultados = new List<BlocoGuardado>();
            foreach (var pedido in pedidos)
            {
                var (saida, erro) = await ExecutarAsync(pedido, ferramentas, cancellationToken);
                resultados.Add(new BlocoGuardado("resultado", saida, Id: pedido.Id, Erro: erro ? true : null));
            }

            novas.Add(Mensagem(PapelMensagem.Ferramenta, null, resultados));
        }

        LogLimiteDeChamadas(logger, _opcoes.MaximoDeChamadasPorTurno);
        novas.Add(Mensagem(PapelMensagem.Assistente, "Fiz as atualizações. Quer conferir o resumo antes de seguir?", [new BlocoGuardado("texto", "Fiz as atualizações. Quer conferir o resumo antes de seguir?")]));
        return novas;
    }

    private static MensagemDaConversa Mensagem(PapelMensagem papel, string? texto, IReadOnlyList<BlocoGuardado> blocos)
        => new(papel, string.IsNullOrWhiteSpace(texto) ? null : texto, new ConteudoGuardado(blocos).Gravar());

    private static List<BlocoGuardado> Guardar(BetaMessage resposta)
    {
        var blocos = new List<BlocoGuardado>();
        foreach (var bloco in resposta.Content)
        {
            if (bloco.TryPickText(out var texto))
            {
                blocos.Add(new BlocoGuardado("texto", texto.Text));
            }
            else if (bloco.TryPickThinking(out var pensamento))
            {
                blocos.Add(new BlocoGuardado("pensamento", pensamento.Thinking, pensamento.Signature));
            }
            else if (bloco.TryPickRedactedThinking(out var oculto))
            {
                blocos.Add(new BlocoGuardado("pensamentoOculto", oculto.Data));
            }
            else if (bloco.TryPickToolUse(out var uso))
            {
                blocos.Add(new BlocoGuardado("ferramenta", Id: uso.ID, Nome: uso.Name, Entrada: uso.Input.ToDictionary(kv => kv.Key, kv => kv.Value)));
            }
        }

        return blocos;
    }

    private static BetaMessageParam Reconstruir(MensagemDaConversa mensagem)
    {
        var blocos = new List<BetaContentBlockParam>();
        foreach (var bloco in ConteudoGuardado.Ler(mensagem.Conteudo).Blocos)
        {
            BetaContentBlockParam? param = bloco.Tipo switch
            {
                "texto" => new BetaTextBlockParam { Text = bloco.Texto! },
                "pensamento" => new BetaThinkingBlockParam { Thinking = bloco.Texto!, Signature = bloco.Assinatura! },
                "pensamentoOculto" => new BetaRedactedThinkingBlockParam { Data = bloco.Texto! },
                "ferramenta" => new BetaToolUseBlockParam { ID = bloco.Id!, Name = bloco.Nome!, Input = bloco.Entrada! },
                "resultado" => new BetaToolResultBlockParam { ToolUseID = bloco.Id!, Content = bloco.Texto!, IsError = bloco.Erro },
                _ => null,
            };
            if (param is not null)
            {
                blocos.Add(param);
            }
        }

        return mensagem.Papel == PapelMensagem.Assistente
            ? new BetaAssistantMessageParam { Content = blocos }
            : new BetaUserMessageParam { Content = blocos };
    }

    private static async Task<(string Saida, bool Erro)> ExecutarAsync(BlocoGuardado pedido, IFerramentasDoBriefing ferramentas, CancellationToken cancellationToken)
    {
        var entrada = pedido.Entrada ?? [];
        switch (pedido.Nome)
        {
            case Ferramentas.AtualizarBriefing:
                DateOnly? prazo = null;
                if (Texto(entrada, "prazo") is { } prazoTexto)
                {
                    if (!DateOnly.TryParseExact(prazoTexto, "dd/MM/yyyy", PtBr, DateTimeStyles.None, out var data))
                    {
                        return ($"Prazo inválido: use o formato dd/MM/aaaa (recebido: {prazoTexto}).", true);
                    }

                    prazo = data;
                }

                var refinamento = new Domain.Entities.Briefings.RefinamentoBriefing(
                    OQueSeraFeito: Texto(entrada, "oQueSeraFeito"),
                    Materiais: Texto(entrada, "materiais"),
                    Medidas: Texto(entrada, "medidas"),
                    Acabamento: Texto(entrada, "acabamento"),
                    Pedras: Texto(entrada, "pedras"),
                    Fecho: Texto(entrada, "fecho"),
                    Prazo: prazo,
                    Orcamento: Texto(entrada, "orcamento"),
                    Observacoes: Texto(entrada, "observacoes"));
                return (ferramentas.Refinar(refinamento), false);

            case Ferramentas.GerarPdf:
                try
                {
                    return (await ferramentas.GerarPdfAsync(cancellationToken), false);
                }
                catch (ServicoIndisponivelException ex)
                {
                    return ($"Não foi possível gerar o PDF: {ex.Message}", true);
                }

            default:
                return ($"Ferramenta desconhecida: {pedido.Nome}.", true);
        }
    }

    private static string? Texto(Dictionary<string, JsonElement> entrada, string campo)
        => entrada.TryGetValue(campo, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Turno do briefing atingiu o limite de {Limite} chamadas à IA.")]
    private static partial void LogLimiteDeChamadas(ILogger logger, int limite);

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao chamar a IA do briefing.")]
    private static partial void LogFalhaDaIa(ILogger logger, Exception ex);

    private async Task<BetaMessage> ChamarAsync(AnthropicClient cliente, string contexto, IReadOnlyList<MensagemDaConversa> conversa, CancellationToken cancellationToken)
    {
        try
        {
            return await cliente.Beta.Messages.Create(
                new MessageCreateParams
                {
                    Model = _opcoes.Modelo,
                    MaxTokens = 16000,

                    // Se a IA recusar por um classificador de segurança, a API tenta outro modelo sozinha.
                    Betas = ["server-side-fallback-2026-07-01"],
                    Fallbacks = new Default(),
                    System = new List<BetaTextBlockParam>
                    {
                        new() { Text = Instrucoes.Sistema },
                        new() { Text = "Estado atual do briefing (JSON):\n" + contexto },
                    },
                    Tools = Ferramentas.Todas,
                    Messages = [.. conversa.Select(Reconstruir)],
                },
                cancellationToken);
        }
        catch (AnthropicException ex)
        {
            LogFalhaDaIa(logger, ex);
            throw new ServicoIndisponivelException("A IA não respondeu agora. Tente de novo em instantes.", ex);
        }
    }

    /// <summary>
    /// Definição das ferramentas. <c>strict</c> garante que a entrada siga o esquema.
    /// </summary>
    private static class Ferramentas
    {
        public const string AtualizarBriefing = "atualizar_briefing";
        public const string GerarPdf = "gerar_pdf";

        public static readonly BetaToolUnion[] Todas =
        [
            new BetaTool
            {
                Name = AtualizarBriefing,
                Description = "Grava no briefing os campos que o joalheiro confirmou. Campos null não são alterados. Prazo no formato dd/MM/aaaa.",
                Strict = true,
                InputSchema = Esquema(CamposDoRefinamento.ToDictionary(
                    c => c,
                    _ => JsonSerializer.SerializeToElement(new { type = new[] { "string", "null" } }))),
            },
            new BetaTool
            {
                Name = GerarPdf,
                Description = "Gera o PDF do briefing com os dados gravados e o guarda no projeto para download.",
                Strict = true,
                InputSchema = Esquema([]),
            },
        ];

        /// <summary>
        /// O construtor tipado não expõe <c>additionalProperties</c>, exigido por <c>strict</c>: monta o esquema cru.
        /// </summary>
        private static InputSchema Esquema(Dictionary<string, JsonElement> propriedades) => InputSchema.FromRawUnchecked(new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(propriedades),
            ["required"] = JsonSerializer.SerializeToElement(propriedades.Keys),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        });
    }
}

using System.Text;
using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Ferramentas.Briefings;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// IA das ferramentas de mentira (sem API paga nem CADCraft). "falhe" simula indisponibilidade; uma fala
/// com "desenhe" muda o arquivo (o novo conteúdo é "DXF:" + a fala) e gera um render; qualquer outra só conversa.
/// Como a real, publica o andamento (pensamento, um passo e o texto) e informa o consumo do turno.
/// No briefing usa as ferramentas locais: sem fala abre a conversa; com "pdf" gera o PDF; qualquer outra fala
/// vira o "o que será feito".
/// </summary>
public sealed class AssistenteFerramentaDeTeste : IAssistenteFerramenta
{
    /// <summary>
    /// PNG mínimo (assinatura reconhecida pelo domínio).
    /// </summary>
    public static readonly byte[] Render = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];

    public static readonly ConsumoDoTurno Consumo = new("claude-teste", 2, 1200, 300, 800, 100);

    /// <summary>
    /// Último pedido recebido, para os testes conferirem o contexto e o arquivo enviados à IA.
    /// </summary>
    public static PedidoAFerramenta? UltimoPedido { get; private set; }

    public IReadOnlyCollection<string> Ferramentas { get; } = ["desenho-2d", "briefing"];

    public async Task<TurnoDaFerramenta> ConversarAsync(PedidoAFerramenta pedido, CancellationToken cancellationToken)
    {
        UltimoPedido = pedido;
        if (pedido.Fala == "falhe")
        {
            throw new ServicoIndisponivelException("A IA não respondeu agora. Tente de novo em instantes.");
        }

        if (pedido.Ferramenta == "briefing")
        {
            return await BriefingAsync(pedido, cancellationToken);
        }

        var observar = pedido.Observar ?? (_ => { });
        observar(new EventoDoTurno("pensamento", "Pensando no pedido."));
        var fala = new MensagemDoTurno(PapelMensagem.Joalheiro, pedido.Fala, Conteudo(pedido.Fala ?? "imagem"), pedido.ImagensDaFala, null);
        if (pedido.Fala?.Contains("desenhe", StringComparison.OrdinalIgnoreCase) != true)
        {
            observar(new EventoDoTurno("texto", "Anotado."));
            return new TurnoDaFerramenta([fala, Resposta("Anotado.", [], null, [])], [], null, Consumo);
        }

        var render = new ImagemDaConversa(Guid.CreateVersion7(), "image/png", Render);
        observar(new EventoDoTurno("passo", "Desenhando círculos", 1, "andamento"));
        observar(new EventoDoTurno("passo", null, 1, "feito", Convert.ToBase64String(Render)));
        observar(new EventoDoTurno("texto", "Desenhei a peça."));
        var turno = new TurnoDaFerramenta(
            [fala, Resposta("Desenhei a peça.", [render.Chave], "Desenhei a peça", ["Desenhando círculos"])],
            [render],
            Encoding.UTF8.GetBytes("DXF:" + pedido.Fala),
            Consumo);
        return turno;
    }

    private static async Task<TurnoDaFerramenta> BriefingAsync(PedidoAFerramenta pedido, CancellationToken cancellationToken)
    {
        var locais = pedido.Locais!;
        var fala = new MensagemDoTurno(PapelMensagem.Joalheiro, pedido.Fala, Conteudo(pedido.Fala ?? "abertura"), pedido.ImagensDaFala, null);
        string resposta;
        if (pedido.Fala is null)
        {
            resposta = "Olá! Qual a medida?";
        }
        else if (pedido.Fala.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            await locais.ExecutarAsync("gerar_pdf", new Dictionary<string, JsonElement>(), cancellationToken);
            resposta = "PDF pronto.";
        }
        else
        {
            await locais.ExecutarAsync("atualizar_briefing", new Dictionary<string, JsonElement> { ["oQueSeraFeito"] = JsonSerializer.SerializeToElement(pedido.Fala) }, cancellationToken);
            resposta = "Anotado.";
        }

        return new TurnoDaFerramenta([fala, Resposta(resposta, [], null, [])], [], null, Consumo);
    }

    private static MensagemDoTurno Resposta(string texto, IReadOnlyList<Guid> imagens, string? acao, IReadOnlyList<string> passos)
        => new(PapelMensagem.Assistente, texto, Conteudo(texto), imagens, acao, "Pensando no pedido.", passos);

    private static string Conteudo(string texto) => JsonSerializer.Serialize(new { texto });
}

using System.Text;
using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Entities.Briefings;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// IA das ferramentas de mentira (sem API paga nem CADCraft). "falhe" simula indisponibilidade; uma fala
/// com "desenhe" muda o arquivo (o novo conteúdo é "DXF:" + a fala) e gera um render; qualquer outra só conversa.
/// </summary>
public sealed class AssistenteFerramentaDeTeste : IAssistenteFerramenta
{
    /// <summary>
    /// PNG mínimo (assinatura reconhecida pelo domínio).
    /// </summary>
    public static readonly byte[] Render = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];

    /// <summary>
    /// Último pedido recebido, para os testes conferirem o contexto e o arquivo enviados à IA.
    /// </summary>
    public static PedidoAFerramenta? UltimoPedido { get; private set; }

    public IReadOnlyCollection<string> Ferramentas { get; } = ["desenho-2d"];

    public Task<TurnoDaFerramenta> ConversarAsync(PedidoAFerramenta pedido, CancellationToken cancellationToken)
    {
        UltimoPedido = pedido;
        if (pedido.Fala == "falhe")
        {
            throw new ServicoIndisponivelException("A IA não respondeu agora. Tente de novo em instantes.");
        }

        var fala = new MensagemDoTurno(PapelMensagem.Joalheiro, pedido.Fala, Conteudo(pedido.Fala ?? "imagem"), pedido.ImagensDaFala, null);
        if (pedido.Fala?.Contains("desenhe", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Task.FromResult(new TurnoDaFerramenta([fala, Resposta("Anotado.", [], null)], [], null));
        }

        var render = new ImagemDaConversa(Guid.CreateVersion7(), "image/png", Render);
        var turno = new TurnoDaFerramenta(
            [fala, Resposta("Desenhei a peça.", [render.Chave], "Desenhei a peça")],
            [render],
            Encoding.UTF8.GetBytes("DXF:" + pedido.Fala));
        return Task.FromResult(turno);
    }

    private static MensagemDoTurno Resposta(string texto, IReadOnlyList<Guid> imagens, string? acao)
        => new(PapelMensagem.Assistente, texto, Conteudo(texto), imagens, acao);

    private static string Conteudo(string texto) => JsonSerializer.Serialize(new { texto });
}

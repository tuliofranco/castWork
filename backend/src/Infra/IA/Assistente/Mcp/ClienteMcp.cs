using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Castwork.Infra.IA.Assistente.Mcp;

/// <summary>
/// Cliente MCP mínimo sobre stdio (JSON-RPC, uma mensagem por linha): inicia o servidor da ferramenta,
/// lista as ferramentas e as chama. Um processo por turno da conversa, encerrado no fim.
/// </summary>
internal sealed class ClienteMcp : IAsyncDisposable
{
    private const string VersaoDoProtocolo = "2025-06-18";

    private readonly Process _processo;
    private readonly TimeSpan _tempoLimite;
    private int _proximoId;

    private ClienteMcp(Process processo, TimeSpan tempoLimite)
    {
        _processo = processo;
        _tempoLimite = tempoLimite;
    }

    public static async Task<ClienteMcp> IniciarAsync(string executavel, IReadOnlyList<string> argumentos, string pastaDeTrabalho, TimeSpan tempoLimite, CancellationToken cancellationToken)
    {
        var inicio = new ProcessStartInfo(executavel, argumentos)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = pastaDeTrabalho,
        };
        var processo = Process.Start(inicio) ?? throw new InvalidOperationException($"Não foi possível iniciar {executavel}.");

        // O stderr é log do servidor: lido e descartado para o buffer nunca encher e travar o processo.
        _ = processo.StandardError.ReadToEndAsync(CancellationToken.None);

        var cliente = new ClienteMcp(processo, tempoLimite);
        await cliente.ChamarAsync(
            "initialize",
            new JsonObject
            {
                ["protocolVersion"] = VersaoDoProtocolo,
                ["capabilities"] = new JsonObject(),
                ["clientInfo"] = new JsonObject { ["name"] = "castwork", ["version"] = "1.0" },
            },
            cancellationToken);
        await cliente.EscreverAsync(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = "notifications/initialized" }, cancellationToken);
        return cliente;
    }

    public async Task<IReadOnlyList<FerramentaMcp>> ListarFerramentasAsync(CancellationToken cancellationToken)
    {
        var resultado = await ChamarAsync("tools/list", new JsonObject(), cancellationToken);
        return resultado.GetProperty("tools").EnumerateArray()
            .Select(t => new FerramentaMcp(
                t.GetProperty("name").GetString()!,
                t.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty,
                t.GetProperty("inputSchema").Clone()))
            .ToList();
    }

    public async Task<ResultadoMcp> ChamarFerramentaAsync(string nome, JsonElement argumentos, CancellationToken cancellationToken)
    {
        var resultado = await ChamarAsync(
            "tools/call",
            new JsonObject { ["name"] = nome, ["arguments"] = JsonNode.Parse(argumentos.GetRawText()) },
            cancellationToken);

        var conteudo = new List<ConteudoMcp>();
        if (resultado.TryGetProperty("content", out var blocos))
        {
            foreach (var bloco in blocos.EnumerateArray())
            {
                switch (bloco.GetProperty("type").GetString())
                {
                    case "text":
                        conteudo.Add(new ConteudoMcp(bloco.GetProperty("text").GetString(), null, null));
                        break;
                    case "image":
                        conteudo.Add(new ConteudoMcp(null, Convert.FromBase64String(bloco.GetProperty("data").GetString()!), bloco.GetProperty("mimeType").GetString()));
                        break;
                }
            }
        }

        var erro = resultado.TryGetProperty("isError", out var e) && e.ValueKind == JsonValueKind.True;
        return new ResultadoMcp(conteudo, erro);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _processo.StandardInput.Close();
            using var espera = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _processo.WaitForExitAsync(espera.Token);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException or IOException)
        {
            _processo.Kill(entireProcessTree: true);
        }
        finally
        {
            _processo.Dispose();
        }
    }

    private async Task<JsonElement> ChamarAsync(string metodo, JsonObject parametros, CancellationToken cancellationToken)
    {
        var id = ++_proximoId;
        await EscreverAsync(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = metodo, ["params"] = parametros }, cancellationToken);

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(_tempoLimite);
        while (true)
        {
            var linha = await _processo.StandardOutput.ReadLineAsync(limite.Token)
                ?? throw new IOException("O servidor MCP encerrou sem responder.");
            if (string.IsNullOrWhiteSpace(linha))
            {
                continue;
            }

            using var documento = JsonDocument.Parse(linha);
            var raiz = documento.RootElement;

            // Notificações e pedidos do servidor (sem o nosso id) são ignorados.
            if (!raiz.TryGetProperty("id", out var idDaResposta) || idDaResposta.ValueKind != JsonValueKind.Number || idDaResposta.GetInt32() != id)
            {
                continue;
            }

            if (raiz.TryGetProperty("error", out var erro))
            {
                throw new IOException($"MCP {metodo}: {erro.GetProperty("message").GetString()}");
            }

            return raiz.GetProperty("result").Clone();
        }
    }

    private async Task EscreverAsync(JsonObject mensagem, CancellationToken cancellationToken)
    {
        await _processo.StandardInput.WriteLineAsync(mensagem.ToJsonString().AsMemory(), cancellationToken);
        await _processo.StandardInput.FlushAsync(cancellationToken);
    }
}

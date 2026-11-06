using System.Text.Json;

using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Entities.Briefings;
using Castwork.Infra.IA.Briefing;
using Castwork.Infra.IA.CadCraft;
using Castwork.Infra.IA.Conversa;
using Castwork.Infra.IA.Mcp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.IA.Ferramentas;

/// <summary>
/// Conduz a conversa de uma ferramenta com a Claude. A cada turno sobe o servidor MCP da ferramenta, abre
/// o arquivo da versão, oferece à IA as ferramentas desse servidor (e só elas) e repassa os pedidos até a
/// resposta final. Se a IA mudou o desenho, salva e devolve o arquivo novo.
/// </summary>
internal sealed partial class AssistenteFerramentaClaude(
    IOptions<AnthropicOptions> anthropic,
    IOptions<CadCraftOptions> cadCraft,
    ILogger<AssistenteFerramentaClaude> logger)
    : IAssistenteFerramenta
{
    /// <summary>
    /// Retorno de ferramenta longo (a lista de entidades de um desenho grande) é cortado: a IA pede por partes.
    /// </summary>
    private const int TextoMaximoDoResultado = 20_000;

    /// <summary>
    /// Ferramenta do próprio Castwork (não vai ao MCP): a IA resume o que mudou, para o cartão do chat.
    /// </summary>
    private const string ResumirAlteracao = "resumir_alteracao";

    private static readonly BetaToolUnion FerramentaDeResumo = new BetaTool
    {
        Name = ResumirAlteracao,
        Description = "Registra um resumo curto (até 80 caracteres) do que você alterou no desenho neste turno. Chame uma vez, no fim, só se alterou o desenho.",
        Strict = true,
        InputSchema = InputSchema.FromRawUnchecked(new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(new { resumo = new { type = "string" } }),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "resumo" }),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        }),
    };

    private readonly AnthropicOptions _opcoes = anthropic.Value;

    private readonly Dictionary<string, DefinicaoDeFerramenta> _definicoes = new()
    {
        [Desenho2d.Codigo] = Desenho2d.Criar(cadCraft.Value),
        [BriefingFerramenta.Codigo] = BriefingFerramenta.Criar(),
    };

    public IReadOnlyCollection<string> Ferramentas => _definicoes.Keys;

    public async Task<TurnoDaFerramenta> ConversarAsync(PedidoAFerramenta pedido, CancellationToken cancellationToken)
    {
        var definicao = _definicoes[pedido.Ferramenta];
        if (string.IsNullOrWhiteSpace(_opcoes.ApiKey))
        {
            throw new ServicoIndisponivelException("A IA não está configurada neste ateliê.");
        }

        // Sem programa externo (o briefing): só as ferramentas locais, e nenhum arquivo para salvar.
        if (definicao.SemMcp)
        {
            var semMcp = await ConduzirAsync(pedido, definicao, null, [.. Locais(pedido)], cancellationToken);
            return new TurnoDaFerramenta(semMcp.Mensagens, semMcp.Imagens, null, semMcp.Consumo);
        }

        if (string.IsNullOrWhiteSpace(definicao.Executavel) || !File.Exists(definicao.Executavel))
        {
            throw new ServicoIndisponivelException("O programa desta ferramenta não está configurado neste ateliê.");
        }

        var pasta = Directory.CreateTempSubdirectory("castwork-ferramenta-");
        try
        {
            await using var mcp = await IniciarAsync(definicao, pasta.FullName, cancellationToken);
            await PrepararArquivoAsync(mcp, definicao, pasta.FullName, pedido.Arquivo, cancellationToken);
            BetaToolUnion[] ferramentas =
            [
                .. (await mcp.ListarFerramentasAsync(cancellationToken)).Where(f => !definicao.FerramentasOcultas.Contains(f.Nome)).Select(ParaClaude),
                FerramentaDeResumo,
                .. Locais(pedido),
            ];

            var turno = await ConduzirAsync(pedido, definicao, mcp, ferramentas, cancellationToken);
            if (!turno.Alterou)
            {
                return new TurnoDaFerramenta(turno.Mensagens, turno.Imagens, null, turno.Consumo);
            }

            var saida = Path.Combine(pasta.FullName, "saida" + definicao.ExtensaoDoArquivo);
            var (nome, argumentos) = definicao.SalvarArquivo(saida);
            var salvo = await mcp.ChamarFerramentaAsync(nome, JsonDocument.Parse(argumentos).RootElement, cancellationToken);
            if (salvo.Erro || !File.Exists(saida))
            {
                LogFalhaAoSalvar(logger, salvo.Texto);
                throw new ServicoIndisponivelException("A IA desenhou, mas não foi possível salvar o arquivo. Tente de novo.");
            }

            return new TurnoDaFerramenta(turno.Mensagens, turno.Imagens, await File.ReadAllBytesAsync(saida, cancellationToken), turno.Consumo);
        }
        catch (IOException ex)
        {
            LogFalhaDoMcp(logger, ex);
            throw new ServicoIndisponivelException("O programa da ferramenta não respondeu. Tente de novo em instantes.", ex);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    /// <summary>
    /// As ferramentas locais da Application, em modo estrito (o esquema delas é escrito para isso).
    /// </summary>
    private static IEnumerable<BetaToolUnion> Locais(PedidoAFerramenta pedido)
        => (pedido.Locais?.Definicoes ?? []).Select(f => (BetaToolUnion)new BetaTool
        {
            Name = f.Nome,
            Description = f.Descricao,
            Strict = true,
            InputSchema = InputSchema.FromRawUnchecked(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(f.EsquemaJson)!),
        });

    private static async Task<ClienteMcp> IniciarAsync(DefinicaoDeFerramenta definicao, string pasta, CancellationToken cancellationToken)
        => await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta, TimeSpan.FromSeconds(60), cancellationToken);

    private static async Task PrepararArquivoAsync(ClienteMcp mcp, DefinicaoDeFerramenta definicao, string pasta, byte[]? arquivo, CancellationToken cancellationToken)
    {
        (string Nome, string Argumentos) abrir;
        if (arquivo is null)
        {
            abrir = definicao.NovoArquivo;
        }
        else
        {
            var caminho = Path.Combine(pasta, "atual" + definicao.ExtensaoDoArquivo);
            await File.WriteAllBytesAsync(caminho, arquivo, cancellationToken);
            abrir = definicao.AbrirArquivo(caminho);
        }

        var resultado = await mcp.ChamarFerramentaAsync(abrir.Nome, JsonDocument.Parse(abrir.Argumentos).RootElement, cancellationToken);
        if (resultado.Erro)
        {
            throw new IOException($"Não foi possível abrir o arquivo: {resultado.Texto}");
        }
    }

    /// <summary>
    /// A ferramenta do MCP vira ferramenta da Claude com o mesmo nome, descrição e esquema. Sem <c>strict</c>:
    /// os esquemas do servidor não seguem as exigências do modo estrito.
    /// </summary>
    private static BetaToolUnion ParaClaude(FerramentaMcp ferramenta) => new BetaTool
    {
        Name = ferramenta.Nome,
        Description = ferramenta.Descricao,
        InputSchema = InputSchema.FromRawUnchecked(ferramenta.Esquema.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone())),
    };

    /// <summary>
    /// A primeira frase da resposta da IA vira o resumo do cartão ("Aplicou: ...").
    /// </summary>
    private static string ResumoDaAcao(string texto)
    {
        var frase = texto.Split(['.', '\n'], 2, StringSplitOptions.TrimEntries)[0];
        return string.IsNullOrWhiteSpace(frase) ? "alterou o desenho" : frase.Length > 120 ? frase[..117] + "..." : frase;
    }

    private static string? Texto(Dictionary<string, JsonElement>? entrada, string campo)
        => entrada is not null && entrada.TryGetValue(campo, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;

    private static MensagemDoTurno Mensagem(
        PapelMensagem papel,
        string? texto,
        IReadOnlyList<BlocoGuardado> blocos,
        IReadOnlyList<Guid>? imagens = null,
        string? acao = null,
        Andamento? andamento = null)
        => new(
            papel,
            string.IsNullOrWhiteSpace(texto) ? null : texto,
            new ConteudoGuardado(blocos).Gravar(),
            imagens ?? [],
            acao,
            andamento?.Pensamento,
            andamento?.Passos);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Turno da ferramenta atingiu o limite de {Limite} chamadas à IA.")]
    private static partial void LogLimiteDeChamadas(ILogger logger, int limite);

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao chamar a IA da ferramenta.")]
    private static partial void LogFalhaDaIa(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha na comunicação com o servidor MCP da ferramenta.")]
    private static partial void LogFalhaDoMcp(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "O servidor MCP não salvou o arquivo: {Detalhe}")]
    private static partial void LogFalhaAoSalvar(ILogger logger, string detalhe);

    private async Task<(List<MensagemDoTurno> Mensagens, List<ImagemDaConversa> Imagens, bool Alterou, ConsumoDoTurno Consumo)> ConduzirAsync(
        PedidoAFerramenta pedido,
        DefinicaoDeFerramenta definicao,
        ClienteMcp? mcp,
        BetaToolUnion[] ferramentas,
        CancellationToken cancellationToken)
    {
        var cliente = new AnthropicClient { ApiKey = _opcoes.ApiKey };
        var imagens = new Dictionary<Guid, ImagemDaConversa>(pedido.Imagens);
        var geradas = new List<ImagemDaConversa>();
        var andamento = new Andamento(pedido.Observar ?? (_ => { }), _opcoes.Modelo);
        var alterou = false;
        string? resumo = null;

        // As imagens vêm antes do texto, como a documentação da API recomenda.
        var blocosDaFala = pedido.ImagensDaFala.Select(c => new BlocoGuardado("imagem", Id: c.ToString())).ToList();
        if (pedido.Fala is not null)
        {
            blocosDaFala.Add(new BlocoGuardado("texto", pedido.Fala));
        }
        else if (blocosDaFala.Count == 0)
        {
            // Abertura: a IA começa a conversa; a fala fica oculta no chat (sem texto nem imagem).
            blocosDaFala.Add(new BlocoGuardado("texto", definicao.Abertura ?? "Comece a conversa."));
        }

        var novas = new List<MensagemDaConversa>();
        var doTurno = new List<MensagemDoTurno> { Mensagem(PapelMensagem.Joalheiro, pedido.Fala, blocosDaFala, pedido.ImagensDaFala) };
        novas.Add(new MensagemDaConversa(PapelMensagem.Joalheiro, pedido.Fala, doTurno[0].Conteudo));

        for (var chamada = 0; chamada < _opcoes.MaximoDeChamadasPorTurnoNasFerramentas; chamada++)
        {
            var resposta = await ChamarAsync(cliente, definicao, pedido.Contexto, [.. pedido.Historico, .. novas], imagens, ferramentas, andamento, cancellationToken);
            var blocos = BlocosDaConversa.Guardar(resposta);
            andamento.Pensou(blocos.Where(b => b.Tipo == "pensamento").Select(b => b.Texto));
            var texto = string.Join("\n\n", blocos.Where(b => b.Tipo == "texto").Select(b => b.Texto));
            var pedidos = blocos.Where(b => b.Tipo == "ferramenta").ToList();

            if (pedidos.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(texto))
                {
                    texto = "Não consegui responder a isso. Pode reformular a mensagem?";
                    blocos = [.. blocos, new BlocoGuardado("texto", texto)];
                }

                // A última imagem que a IA olhou (o render) aparece com a resposta: o joalheiro vê o que ela viu.
                var vista = geradas.Count > 0 ? new[] { geradas[^1].Chave } : null;
                var acao = alterou ? resumo ?? ResumoDaAcao(texto) : null;
                doTurno.Add(Mensagem(PapelMensagem.Assistente, texto, blocos, vista, acao, andamento));
                return (doTurno, geradas, alterou, andamento.Consumo);
            }

            var assistente = Mensagem(PapelMensagem.Assistente, texto, blocos);
            doTurno.Add(assistente);
            novas.Add(new MensagemDaConversa(PapelMensagem.Assistente, assistente.Texto, assistente.Conteudo));

            var resultados = new List<BlocoGuardado>();
            foreach (var uso in pedidos)
            {
                if (uso.Nome == ResumirAlteracao)
                {
                    resumo = Texto(uso.Entrada, "resumo")?.Trim() is { Length: > 0 } r ? (r.Length > 120 ? r[..117] + "..." : r) : resumo;
                    resultados.Add(new BlocoGuardado("resultado", "Resumo registrado.", Id: uso.Id));
                    continue;
                }

                if (pedido.Locais?.Definicoes.FirstOrDefault(f => f.Nome == uso.Nome) is { } local)
                {
                    var passoLocal = andamento.Comecar(local.Passo);
                    var feito = await pedido.Locais.ExecutarAsync(local.Nome, uso.Entrada ?? [], cancellationToken);
                    andamento.Terminar(passoLocal, feito.Erro, null);
                    resultados.Add(new BlocoGuardado("resultado", feito.Texto, Id: uso.Id, Erro: feito.Erro ? true : null));
                    continue;
                }

                if (mcp is null)
                {
                    resultados.Add(new BlocoGuardado("resultado", $"Ferramenta desconhecida: {uso.Nome}.", Id: uso.Id, Erro: true));
                    continue;
                }

                var passo = andamento.Comecar(PassosDoDesenho.Descrever(uso.Nome!, uso.Entrada));
                var argumentos = JsonSerializer.SerializeToElement(uso.Entrada ?? []);
                var resultado = await mcp.ChamarFerramentaAsync(uso.Nome!, argumentos, cancellationToken);
                alterou |= !resultado.Erro && definicao.FerramentasQueAlteram.Contains(uso.Nome!);

                var chaves = new List<string>();
                ImagemDaConversa? vista = null;
                foreach (var imagem in resultado.Conteudo.Where(c => c.Imagem is not null))
                {
                    var gerada = new ImagemDaConversa(Guid.CreateVersion7(), imagem.TipoDaImagem ?? "image/png", imagem.Imagem!);
                    imagens[gerada.Chave] = gerada;
                    geradas.Add(gerada);
                    chaves.Add(gerada.Chave.ToString());
                    vista = gerada;
                }

                andamento.Terminar(passo, resultado.Erro, vista);
                var saida = resultado.Texto.Length > TextoMaximoDoResultado ? resultado.Texto[..TextoMaximoDoResultado] + "\n[cortado]" : resultado.Texto;
                resultados.Add(new BlocoGuardado("resultado", saida, Id: uso.Id, Erro: resultado.Erro ? true : null, Imagens: chaves.Count > 0 ? chaves : null));
            }

            var retorno = Mensagem(PapelMensagem.Ferramenta, null, resultados);
            doTurno.Add(retorno);
            novas.Add(new MensagemDaConversa(PapelMensagem.Ferramenta, null, retorno.Conteudo));
        }

        LogLimiteDeChamadas(logger, _opcoes.MaximoDeChamadasPorTurnoNasFerramentas);
        const string Parou = "Parei aqui para você conferir o desenho. Quer que eu continue?";
        doTurno.Add(Mensagem(PapelMensagem.Assistente, Parou, [new BlocoGuardado("texto", Parou)], acao: alterou ? resumo ?? "alterou o desenho" : null, andamento: andamento));
        return (doTurno, geradas, alterou, andamento.Consumo);
    }

    /// <summary>
    /// Chamada em streaming: o pensamento (resumido) e o texto chegam aos pedaços e vão para o painel do
    /// turno; no fim a mensagem é montada inteira, como se tivesse vindo de uma vez.
    /// </summary>
    private async Task<BetaMessage> ChamarAsync(
        AnthropicClient cliente,
        DefinicaoDeFerramenta definicao,
        string contexto,
        IReadOnlyList<MensagemDaConversa> conversa,
        IReadOnlyDictionary<Guid, ImagemDaConversa> imagens,
        BetaToolUnion[] ferramentas,
        Andamento andamento,
        CancellationToken cancellationToken)
    {
        var parametros = new MessageCreateParams
        {
            Model = _opcoes.Modelo,
            MaxTokens = 16000,

            // O Opus 5.5 sempre pensa; "resumido" faz a API devolver o resumo desse pensamento para mostrar.
            Thinking = new BetaThinkingConfigAdaptive { Display = Display.Summarized },

            // Se a IA recusar por um classificador de segurança, a API tenta outro modelo sozinha.
            Betas = ["server-side-fallback-2026-07-01"],
            Fallbacks = new Default(),
            System = new List<BetaTextBlockParam>
            {
                new() { Text = definicao.Instrucoes },
                new() { Text = "Contexto desta versão da peça (JSON):\n" + contexto },
            },
            Tools = ferramentas,
            Messages = [.. conversa.Select(m => BlocosDaConversa.Reconstruir(m, imagens))],
        };

        try
        {
            andamento.NovaChamada();
            var mensagem = await Acompanhado(cliente.Beta.Messages.CreateStreaming(parametros, cancellationToken), andamento).Aggregate();
            andamento.Somar(mensagem.Usage);
            return mensagem;
        }
        catch (AnthropicException ex)
        {
            LogFalhaDaIa(logger, ex);
            throw new ServicoIndisponivelException(FalhaDaIa.Mensagem(ex), ex);
        }

        // Repassa os trechos de pensamento e de texto ao painel enquanto o agregador do SDK monta a mensagem.
        static async IAsyncEnumerable<BetaRawMessageStreamEvent> Acompanhado(IAsyncEnumerable<BetaRawMessageStreamEvent> eventos, Andamento andamento)
        {
            await foreach (var evento in eventos)
            {
                if (evento.TryPickContentBlockDelta(out var trecho))
                {
                    if (trecho.Delta.TryPickThinking(out var pensamento))
                    {
                        andamento.Pensando(pensamento.Thinking);
                    }
                    else if (trecho.Delta.TryPickText(out var texto))
                    {
                        andamento.Escrevendo(texto.Text);
                    }
                }

                yield return evento;
            }
        }
    }
}

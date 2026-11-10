using System.Text.Json;

using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Entities.Assistente;
using Castwork.Infra.IA.Assistente.Conversa;
using Castwork.Infra.IA.Assistente.Mcp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// Conduz a conversa de uma ferramenta com a Claude. A cada turno sobe o servidor MCP da ferramenta, abre
/// o arquivo da versão, oferece à IA as ferramentas desse servidor (e só elas) e repassa os pedidos até a
/// resposta final. Se a IA mudou o desenho, salva e devolve o arquivo novo.
/// </summary>
internal sealed partial class AssistenteFerramentaClaude(
    IOptions<AnthropicOptions> anthropic,
    IEnumerable<IFerramentaNaIa> registradas,
    ILogger<AssistenteFerramentaClaude> logger)
    : IAssistenteFerramenta
{
    /// <summary>
    /// Retorno de ferramenta longo (a lista de entidades de um desenho grande) é cortado: a IA pede por partes.
    /// </summary>
    private const int TextoMaximoDoResultado = 8_000;

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

    private readonly Dictionary<string, DefinicaoDeFerramenta> _definicoes = registradas.Select(f => f.Definicao).ToDictionary(d => d.Codigo);

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
            var semMcp = await ConduzirAsync(pedido, definicao, null, null, [.. Locais(pedido)], cancellationToken);
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

            // O que a ferramenta pede que o arquivo tenha (ex.: os materiais do ateliê). Se falhar, a IA modela assim mesmo.
            if (await PreparoDoArquivo.AplicarAsync(mcp, definicao, pedido.PreparoDoArquivo, cancellationToken) is { } falhaNoPreparo)
            {
                LogFalhaNoPreparo(logger, falhaNoPreparo);
            }

            BetaToolUnion[] ferramentas =
            [
                .. (await mcp.ListarFerramentasAsync(cancellationToken)).Where(f => !definicao.FerramentasOcultas.Contains(f.Nome)).Select(ParaClaude),
                FerramentaDeResumo,
                .. Locais(pedido),
            ];

            var turno = await ConduzirAsync(pedido, definicao, mcp, pasta.FullName, ferramentas, cancellationToken);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível preparar o arquivo da ferramenta: {Detalhe}")]
    private static partial void LogFalhaNoPreparo(ILogger logger, string detalhe);

    private async Task<(List<MensagemDoTurno> Mensagens, List<ImagemDaConversa> Imagens, bool Alterou, ConsumoDoTurno Consumo)> ConduzirAsync(
        PedidoAFerramenta pedido,
        DefinicaoDeFerramenta definicao,
        ClienteMcp? mcp,
        string? pasta,
        BetaToolUnion[] ferramentas,
        CancellationToken cancellationToken)
    {
        var cliente = new AnthropicClient { ApiKey = _opcoes.ApiKey };
        var imagens = new Dictionary<Guid, ImagemDaConversa>(pedido.Imagens);
        var geradas = new List<ImagemDaConversa>();
        var modelo = (pedido.Modelo ?? definicao.ModeloPadrao) switch
        {
            "haiku" => "claude-haiku-5-5",
            "sonnet" => "claude-sonnet-5-5",
            "opus" => "claude-opus-5-5",
            _ => _opcoes.Modelo,
        };
        var esforco = pedido.Esforco switch
        {
            "low" => Effort.Low,
            "high" => Effort.High,
            _ => Effort.Medium,
        };
        var andamento = new Andamento(pedido.Observar ?? (_ => { }), modelo);
        var alterou = false;
        string? resumo = null;

        // As imagens vêm antes do texto, como a documentação da API recomenda.
        var blocosDaFala = pedido.ImagensDaFala.Select(c => new BlocoGuardado("imagem", Id: c.ToString())).ToList();

        // Os arquivos do projeto anexados vêm logo depois das imagens; os PDFs só vão à IA neste turno.
        var documentos = new Dictionary<Guid, byte[]>();
        blocosDaFala.AddRange(await AnexosDaFala.PrepararAsync(pedido.Anexos ?? [], _definicoes, pasta, documentos, cancellationToken));
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
            var resposta = await ChamarAsync(cliente, modelo, esforco, definicao, pedido.Contexto, [.. BlocosDaConversa.Enxuto(pedido.Historico), .. novas], imagens, documentos, ferramentas, andamento, cancellationToken);
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

                var vista = await VistaAsync(definicao, mcp, alterou, geradas, cancellationToken);
                var acao = alterou ? resumo ?? ResumoDaAcao(texto) : null;
                doTurno.Add(Mensagem(PapelMensagem.Assistente, texto, blocos, vista, acao, andamento));
                return (doTurno, geradas, alterou, andamento.Consumo);
            }

            var assistente = Mensagem(PapelMensagem.Assistente, texto, blocos);
            doTurno.Add(assistente);
            novas.Add(new MensagemDaConversa(PapelMensagem.Assistente, assistente.Texto, assistente.Conteudo));

            var resultados = new List<BlocoGuardado>();
            int? ultimaAlteracao = null;
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

                var entrada = uso.Entrada ?? [];
                if (pedido.EscopoDaVersao && definicao.RecusarNaVersao?.Invoke(uso.Nome!, entrada) is { } recusa)
                {
                    resultados.Add(new BlocoGuardado("resultado", recusa, Id: uso.Id, Erro: true));
                    continue;
                }

                var passo = andamento.Comecar(definicao.DescreverPasso is { } descrever ? descrever(uso.Nome!, uso.Entrada) : "Trabalhando");
                var argumentos = JsonSerializer.SerializeToElement(definicao.AjustarEntrada is { } ajustar ? ajustar(uso.Nome!, entrada) : entrada);
                var resultado = await mcp.ChamarFerramentaAsync(uso.Nome!, argumentos, cancellationToken);
                if (!resultado.Erro && definicao.FerramentasQueAlteram.Contains(uso.Nome!))
                {
                    alterou = true;
                    ultimaAlteracao = resultados.Count;
                }

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
                var compacto = ResultadoCompacto.Compactar(resultado.Texto, definicao.CamposInternos);
                var saida = compacto.Length > TextoMaximoDoResultado ? compacto[..TextoMaximoDoResultado] + "\n[cortado]" : compacto;
                resultados.Add(new BlocoGuardado("resultado", saida, Id: uso.Id, Erro: resultado.Erro ? true : null, Imagens: chaves.Count > 0 ? chaves : null));
            }

            // O arquivo mudou nesta rodada: o resumo dele vai junto do último resultado que o alterou.
            if (ultimaAlteracao is { } indice && mcp is not null && await ResumoDoArquivoAsync(definicao, mcp, cancellationToken) is { } resumoDoArquivo)
            {
                resultados[indice] = resultados[indice] with { Texto = $"{resultados[indice].Texto}\n\n{resumoDoArquivo}" };
            }

            var retorno = Mensagem(PapelMensagem.Ferramenta, null, resultados);
            doTurno.Add(retorno);
            novas.Add(new MensagemDaConversa(PapelMensagem.Ferramenta, null, retorno.Conteudo));
        }

        LogLimiteDeChamadas(logger, _opcoes.MaximoDeChamadasPorTurnoNasFerramentas);
        const string Parou = "Parei aqui para você conferir o desenho. Quer que eu continue?";
        var vistaDoLimite = await VistaAsync(definicao, mcp, alterou, geradas, cancellationToken);
        doTurno.Add(Mensagem(PapelMensagem.Assistente, Parou, [new BlocoGuardado("texto", Parou)], vistaDoLimite, alterou ? resumo ?? "alterou o desenho" : null, andamento));
        return (doTurno, geradas, alterou, andamento.Consumo);
    }

    /// <summary>
    /// Resumo do arquivo depois de uma alteração (ex.: medidas e volume dos corpos); nulo se a ferramenta não tem ou falhou.
    /// </summary>
    private async Task<string?> ResumoDoArquivoAsync(DefinicaoDeFerramenta definicao, ClienteMcp mcp, CancellationToken cancellationToken)
    {
        if (definicao.ResumoDoArquivo is not { } resumo)
        {
            return null;
        }

        try
        {
            var resultado = await mcp.ChamarFerramentaAsync(resumo.Chamada.Nome, JsonDocument.Parse(resumo.Chamada.Argumentos).RootElement, cancellationToken);
            return resultado.Erro ? null : resumo.Resumir(resultado.Texto);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Sem o resumo a IA ainda pode conferir com as próprias ferramentas.
            LogFalhaDoMcp(logger, ex);
            return null;
        }
    }

    /// <summary>
    /// A imagem que aparece com a resposta. Se o arquivo mudou e a ferramenta sabe, o sistema tira uma vista só para
    /// o joalheiro (não vai para a IA); senão, a última imagem que a IA olhou.
    /// </summary>
    private async Task<Guid[]?> VistaAsync(DefinicaoDeFerramenta definicao, ClienteMcp? mcp, bool alterou, List<ImagemDaConversa> geradas, CancellationToken cancellationToken)
    {
        if (alterou && mcp is not null && definicao.VistaParaOJoalheiro is { } chamada)
        {
            try
            {
                var resultado = await mcp.ChamarFerramentaAsync(chamada.Nome, JsonDocument.Parse(chamada.Argumentos).RootElement, cancellationToken);
                if (!resultado.Erro && resultado.Conteudo.FirstOrDefault(c => c.Imagem is not null) is { } imagem)
                {
                    var vista = new ImagemDaConversa(Guid.CreateVersion7(), imagem.TipoDaImagem ?? "image/png", imagem.Imagem!);
                    geradas.Add(vista);
                    return [vista.Chave];
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Sem a vista a resposta continua valendo.
                LogFalhaDoMcp(logger, ex);
            }
        }

        return geradas.Count > 0 ? [geradas[^1].Chave] : null;
    }

    /// <summary>
    /// Chamada em streaming: o pensamento (resumido) e o texto chegam aos pedaços e vão para o painel do
    /// turno; no fim a mensagem é montada inteira, como se tivesse vindo de uma vez.
    /// </summary>
    private async Task<BetaMessage> ChamarAsync(
        AnthropicClient cliente,
        string modelo,
        Effort esforco,
        DefinicaoDeFerramenta definicao,
        string contexto,
        IReadOnlyList<MensagemDaConversa> conversa,
        IReadOnlyDictionary<Guid, ImagemDaConversa> imagens,
        IReadOnlyDictionary<Guid, byte[]> documentos,
        BetaToolUnion[] ferramentas,
        Andamento andamento,
        CancellationToken cancellationToken)
    {
        var parametros = new MessageCreateParams
        {
            Model = modelo,
            MaxTokens = 16000,

            // Pensa só o necessário; "resumido" faz a API devolver o resumo desse pensamento para mostrar.
            Thinking = new BetaThinkingConfigAdaptive { Display = Display.Summarized },
            OutputConfig = new BetaOutputConfig { Effort = esforco },

            // Cache em dois pontos: ferramentas + instruções + contexto (iguais em toda fala da conversa) e,
            // pelo marcador automático, a conversa até o último bloco (as rodadas seguintes do turno leem do cache).
            CacheControl = new BetaCacheControlEphemeral(),
            System = new List<BetaTextBlockParam>
            {
                new() { Text = definicao.Instrucoes },
                new() { Text = "Contexto desta versão da peça (JSON):\n" + contexto, CacheControl = new BetaCacheControlEphemeral() },
            },
            Tools = ferramentas,
            Messages = [.. conversa.Select(m => BlocosDaConversa.Reconstruir(m, imagens, documentos))],
        };

        // Se a IA recusar por um classificador de segurança, a API tenta outro modelo sozinha (o Haiku não tem esse recurso).
        if (!modelo.StartsWith("claude-haiku", StringComparison.Ordinal))
        {
            parametros = parametros with { Betas = ["server-side-fallback-2026-07-01"], Fallbacks = new Default() };
        }

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

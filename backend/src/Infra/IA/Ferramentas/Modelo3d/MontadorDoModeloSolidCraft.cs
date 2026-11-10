using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

using Castwork.Application.Common.Exceptions;
using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Domain.Exceptions;
using Castwork.Infra.IA.Assistente;
using Castwork.Infra.IA.Assistente.Mcp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// Monta o modelo 3D da versão a partir dos modelos das peças pelo <c>solidcraft-cli mcp --headless</c>, sem IA (o mesmo
/// caminho da medição e da conferência), numa pasta temporária: mede a largura de cada peça, abre o arquivo da versão (ou
/// começa um novo), garante os materiais do ateliê (<see cref="PreparoDoArquivo"/>), insere uma cópia da peça por unidade,
/// enfileiradas em X depois do que já está no modelo, solta cada cópia do arquivo de origem, dá aos corpos de cada cópia o
/// nome da unidade ("Elo oval (1)"), acerta os materiais
/// (<see cref="MontagemNoArquivo.AcertarMateriais"/>) e salva.
/// </summary>
internal sealed partial class MontadorDoModeloSolidCraft(IOptions<SolidCraftOptions> opcoes, ILogger<MontadorDoModeloSolidCraft> logger) : IMontadorDoModelo
{
    /// <summary>
    /// Cada unidade leva duas chamadas rápidas; a montagem toda tem até 100 unidades.
    /// </summary>
    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(180);

    public async Task<byte[]> MontarAsync(byte[]? arquivoDaVersao, IReadOnlyList<MaterialDoModelo> materiais, IReadOnlyList<PecaParaMontar> pecas, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pecas);
        var definicao = Modelo3dNaIa.Criar(opcoes.Value);
        if (string.IsNullOrWhiteSpace(definicao.Executavel) || !File.Exists(definicao.Executavel))
        {
            throw new ServicoIndisponivelException("O SolidCraft não está configurado neste ateliê.");
        }

        var pasta = Directory.CreateTempSubdirectory("castwork-montagem-");
        try
        {
            var arquivos = new List<string>();
            for (var i = 0; i < pecas.Count; i++)
            {
                var caminho = Path.Combine(pasta.FullName, $"peca-{i.ToString(CultureInfo.InvariantCulture)}{definicao.ExtensaoDoArquivo}");
                await File.WriteAllBytesAsync(caminho, pecas[i].Arquivo, cancellationToken);
                arquivos.Add(caminho);
            }

            await using var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta.FullName, TempoLimite, cancellationToken);

            // A largura de cada peça, no arquivo dela, para enfileirar as unidades sem uma encostar na outra.
            var faixas = new List<(decimal Min, decimal Max)>();
            for (var i = 0; i < pecas.Count; i++)
            {
                await ExecutarAsync(mcp, definicao.AbrirArquivo(arquivos[i]), $"O modelo 3D da peça {pecas[i].Nome} não abre no SolidCraft.", cancellationToken);
                var medida = await ExecutarAsync(mcp, MedicaoNoArquivo.Medida, $"Não foi possível medir o modelo 3D da peça {pecas[i].Nome}.", cancellationToken);
                faixas.Add(MontagemNoArquivo.FaixaEmX(medida) ?? (0m, 0m));
            }

            if (arquivoDaVersao is null)
            {
                await ExecutarAsync(mcp, definicao.NovoArquivo, "Não foi possível começar o modelo da versão.", cancellationToken);
            }
            else
            {
                var caminho = Path.Combine(pasta.FullName, "versao" + definicao.ExtensaoDoArquivo);
                await File.WriteAllBytesAsync(caminho, arquivoDaVersao, cancellationToken);
                await ExecutarAsync(mcp, definicao.AbrirArquivo(caminho), "O modelo 3D da versão não abre no SolidCraft.", cancellationToken);
            }

            if (await PreparoDoArquivo.AplicarAsync(mcp, definicao, MateriaisDoModelo.ParaPreparo(materiais), cancellationToken) is { } erro)
            {
                LogRecusada(logger, "o preparo dos materiais", erro);
                throw new RegraDeNegocioException("Não foi possível preparar os materiais do ateliê no modelo 3D.");
            }

            // A fila começa depois do que o modelo da versão já tem.
            var existente = MontagemNoArquivo.FaixaEmX(await ExecutarAsync(mcp, MedicaoNoArquivo.Medida, "Não foi possível medir o modelo 3D da versão.", cancellationToken));
            var cursor = existente is { } e ? e.Max + MontagemNoArquivo.Espaco : 0m;

            var ligaDoCorpo = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < pecas.Count; i++)
            {
                var (min, max) = faixas[i];
                for (var unidade = 1; unidade <= pecas[i].Quantidade; unidade++)
                {
                    var nome = NomesDosCorpos.DaUnidade(pecas[i].Nome, unidade);
                    var falha = $"Não foi possível trazer o modelo da peça {pecas[i].Nome} para a versão.";
                    var insercao = MontagemNoArquivo.LerInsercao(await ExecutarAsync(mcp, MontagemNoArquivo.Inserir(Path.GetFileName(arquivos[i]), nome, cursor - min), falha, cancellationToken))
                        ?? throw new RegraDeNegocioException(falha);
                    await ExecutarAsync(mcp, MontagemNoArquivo.Desligar(insercao.Componente), falha, cancellationToken);
                    var (renomear, corpos) = MontagemNoArquivo.RenomearCorpos(insercao.Corpos, nome);
                    if (renomear is { } chamada)
                    {
                        await ExecutarAsync(mcp, chamada, $"Não foi possível dar o nome da peça {pecas[i].Nome} aos corpos dela na versão.", cancellationToken);
                    }

                    foreach (var corpo in corpos)
                    {
                        ligaDoCorpo[corpo] = pecas[i].MaterialDaLiga ?? string.Empty;
                    }

                    cursor += (max - min) + MontagemNoArquivo.Espaco;
                }
            }

            var montada = await ExecutarAsync(mcp, MedicaoNoArquivo.Medida, "Não foi possível medir o modelo montado.", cancellationToken);
            if (MontagemNoArquivo.AcertarMateriais(montada, ligaDoCorpo, materiais) is { } acerto)
            {
                await ExecutarAsync(mcp, acerto, "Não foi possível acertar os materiais das peças no modelo da versão.", cancellationToken);
            }

            var salvo = Path.Combine(pasta.FullName, "montado" + definicao.ExtensaoDoArquivo);
            await ExecutarAsync(mcp, definicao.SalvarArquivo(salvo), "Não foi possível salvar o modelo montado.", cancellationToken);
            return await File.ReadAllBytesAsync(salvo, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception or JsonException or UnauthorizedAccessException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogFalha(logger, ex);
            throw new ServicoIndisponivelException("O SolidCraft não respondeu. Tente de novo em instantes.", ex);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível montar o modelo 3D da versão a partir das peças.")]
    private static partial void LogFalha(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "O SolidCraft recusou {Etapa} ao montar o modelo 3D a partir das peças: {Detalhe}")]
    private static partial void LogRecusada(ILogger logger, string etapa, string detalhe);

    /// <summary>
    /// Recusa do SolidCraft: fica no log, e o joalheiro recebe a mensagem em português.
    /// </summary>
    private async Task<string> ExecutarAsync(ClienteMcp mcp, (string Nome, string Argumentos) chamada, string mensagem, CancellationToken cancellationToken)
    {
        var resultado = await mcp.ChamarFerramentaAsync(chamada.Nome, JsonDocument.Parse(chamada.Argumentos).RootElement, cancellationToken);
        if (resultado.Erro)
        {
            LogRecusada(logger, chamada.Nome, resultado.Texto);
            throw new RegraDeNegocioException(mensagem);
        }

        return resultado.Texto;
    }
}

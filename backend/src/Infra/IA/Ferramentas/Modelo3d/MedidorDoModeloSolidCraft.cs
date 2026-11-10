using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Infra.IA.Assistente;
using Castwork.Infra.IA.Assistente.Mcp;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// Mede o arquivo 3D pelo <c>solidcraft-cli mcp --headless</c>, sem IA: abre o arquivo numa pasta temporária, garante os
/// materiais do ateliê (o mesmo preparo do turno da IA, <see cref="PreparoDoArquivo"/>), roda o <c>measure</c> e o
/// <c>inspect.interference</c> e lê o resultado. Nada é gravado no arquivo. A medição (uns 1 s) fica em memória pelo id
/// do arquivo e pelos materiais: abrir a tela de novo não mede de novo; salvar o arquivo (id novo) ou mudar a densidade de
/// uma liga, sim. Falha não é guardada: a próxima leitura tenta de novo.
/// </summary>
internal sealed partial class MedidorDoModeloSolidCraft(IOptions<SolidCraftOptions> opcoes, IMemoryCache cache, ILogger<MedidorDoModeloSolidCraft> logger)
    : IMedidorDoModelo
{
    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(30);

    private static readonly MemoryCacheEntryOptions Validade = new() { SlidingExpiration = TimeSpan.FromDays(1) };

    public async Task<MedicaoDoModelo?> MedirAsync(ArquivoParaMedir arquivo, IReadOnlyList<MaterialDoModelo> materiais, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arquivo);
        var preparo = MateriaisDoModelo.ParaPreparo(materiais);
        var chave = Chave(arquivo.Id, preparo);
        if (cache.TryGetValue(chave, out MedicaoDoModelo? guardada) && guardada is not null)
        {
            return guardada;
        }

        var definicao = Modelo3dNaIa.Criar(opcoes.Value);
        if (string.IsNullOrWhiteSpace(definicao.Executavel) || !File.Exists(definicao.Executavel))
        {
            LogSemSolidCraft(logger, arquivo.Id);
            return null;
        }

        var medicao = await MedirNoSolidCraftAsync(definicao, arquivo, preparo, cancellationToken);
        if (medicao is not null)
        {
            cache.Set(chave, medicao, Validade);
        }

        return medicao;
    }

    /// <summary>
    /// O arquivo (cada alteração é um id novo) e os materiais que o preparo garante (a densidade muda a massa).
    /// </summary>
    internal static string Chave(long arquivoId, string preparo)
        => $"medicao-3d:{arquivoId}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(preparo)))}";

    [LoggerMessage(Level = LogLevel.Warning, Message = "SolidCraft não configurado (SolidCraft:Caminho): o modelo 3D do arquivo {ArquivoId} não foi medido.")]
    private static partial void LogSemSolidCraft(ILogger logger, long arquivoId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível medir o modelo 3D do arquivo {ArquivoId}.")]
    private static partial void LogFalha(ILogger logger, long arquivoId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "O SolidCraft recusou {Ferramenta} ao medir o arquivo {ArquivoId}: {Detalhe}")]
    private static partial void LogRecusada(ILogger logger, string ferramenta, long arquivoId, string detalhe);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível garantir os materiais ao medir o arquivo {ArquivoId}: {Detalhe}")]
    private static partial void LogMateriais(ILogger logger, long arquivoId, string detalhe);

    private static async Task<ResultadoMcp> ChamarAsync(ClienteMcp mcp, (string Nome, string Argumentos) chamada, CancellationToken cancellationToken)
        => await mcp.ChamarFerramentaAsync(chamada.Nome, JsonDocument.Parse(chamada.Argumentos).RootElement, cancellationToken);

    private async Task<MedicaoDoModelo?> MedirNoSolidCraftAsync(DefinicaoDeFerramenta definicao, ArquivoParaMedir arquivo, string preparo, CancellationToken cancellationToken)
    {
        var pasta = Directory.CreateTempSubdirectory("castwork-medicao-");
        try
        {
            var caminho = Path.Combine(pasta.FullName, "modelo" + definicao.ExtensaoDoArquivo);
            await File.WriteAllBytesAsync(caminho, await arquivo.LerConteudo(cancellationToken), cancellationToken);

            await using var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta.FullName, TempoLimite, cancellationToken);
            var abertura = await ChamarAsync(mcp, definicao.AbrirArquivo(caminho), cancellationToken);
            if (abertura.Erro)
            {
                LogRecusada(logger, "open", arquivo.Id, abertura.Texto);
                return null;
            }

            // Sem os materiais certos, a massa sairia com a densidade antiga: registra e mede assim mesmo.
            if (await PreparoDoArquivo.AplicarAsync(mcp, definicao, preparo, cancellationToken) is { } erro)
            {
                LogMateriais(logger, arquivo.Id, erro);
            }

            var medida = await ChamarAsync(mcp, MedicaoNoArquivo.Medida, cancellationToken);
            var corpos = medida.Erro ? null : MedicaoNoArquivo.LerCorpos(medida.Texto);
            if (corpos is null)
            {
                LogRecusada(logger, MedicaoNoArquivo.Medida.Nome, arquivo.Id, medida.Texto);
                return null;
            }

            var interferencia = await ChamarAsync(mcp, MedicaoNoArquivo.Interferencia, cancellationToken);
            var sobreposicoes = interferencia.Erro ? null : MedicaoNoArquivo.LerSobreposicoes(interferencia.Texto);
            if (sobreposicoes is null)
            {
                LogRecusada(logger, "inspect.interference", arquivo.Id, interferencia.Texto);
                return null;
            }

            return new MedicaoDoModelo(corpos, sobreposicoes);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception or JsonException or UnauthorizedAccessException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogFalha(logger, arquivo.Id, ex);
            return null;
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }
}

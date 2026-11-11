using System.ComponentModel;
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
/// Altera o arquivo 3D pelo <c>solidcraft-cli mcp --headless</c>, sem IA (o mesmo caminho da medição): abre o arquivo numa
/// pasta temporária, garante os materiais do ateliê (<see cref="PreparoDoArquivo"/>), aplica a alteração da conferência
/// (<see cref="EdicaoNoArquivo"/>) e salva. O conteúdo salvo volta para a Application gravar como nova versão do arquivo.
/// </summary>
internal sealed partial class EditorDoModeloSolidCraft(IOptions<SolidCraftOptions> opcoes, ILogger<EditorDoModeloSolidCraft> logger) : IEditorDoModelo
{
    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(30);

    public async Task<byte[]> AlterarAsync(byte[] arquivo, IReadOnlyList<MaterialDoModelo> materiais, AlteracaoDoModelo alteracao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arquivo);
        ArgumentNullException.ThrowIfNull(alteracao);
        var definicao = Modelo3dNaIa.Criar(opcoes.Value);
        if (string.IsNullOrWhiteSpace(definicao.Executavel) || !File.Exists(definicao.Executavel))
        {
            throw new ServicoIndisponivelException("O SolidCraft não está configurado neste ateliê.");
        }

        var pasta = Directory.CreateTempSubdirectory("castwork-conferencia-");
        try
        {
            var caminho = Path.Combine(pasta.FullName, "modelo" + definicao.ExtensaoDoArquivo);
            await File.WriteAllBytesAsync(caminho, arquivo, cancellationToken);

            await using var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta.FullName, TempoLimite, cancellationToken);
            await ExecutarAsync(mcp, definicao.AbrirArquivo(caminho), alteracao, cancellationToken);

            // Sem os materiais do ateliê no arquivo, o material.assign recusaria o nome.
            if (await PreparoDoArquivo.AplicarAsync(mcp, definicao, MateriaisDoModelo.ParaPreparo(materiais), cancellationToken) is { } erro)
            {
                LogRecusada(logger, "o preparo dos materiais", erro);
                throw new RegraDeNegocioException("Não foi possível preparar os materiais do ateliê no modelo 3D.");
            }

            await ExecutarAsync(mcp, EdicaoNoArquivo.Chamada(alteracao), alteracao, cancellationToken);

            var salvo = Path.Combine(pasta.FullName, "alterado" + definicao.ExtensaoDoArquivo);
            await ExecutarAsync(mcp, definicao.SalvarArquivo(salvo), alteracao, cancellationToken);
            return await File.ReadAllBytesAsync(salvo, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception or JsonException or UnauthorizedAccessException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogFalha(logger, EdicaoNoArquivo.Descricao(alteracao), ex);
            throw new ServicoIndisponivelException("O SolidCraft não respondeu. Tente de novo em instantes.", ex);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível {Alteracao} no modelo 3D.")]
    private static partial void LogFalha(ILogger logger, string alteracao, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "O SolidCraft recusou {Etapa} na conferência do modelo 3D: {Detalhe}")]
    private static partial void LogRecusada(ILogger logger, string etapa, string detalhe);

    /// <summary>
    /// Recusa do SolidCraft (corpo que não existe, união que o kernel não fecha): fica no log, e o joalheiro recebe a
    /// mensagem em português.
    /// </summary>
    private async Task ExecutarAsync(ClienteMcp mcp, (string Nome, string Argumentos) chamada, AlteracaoDoModelo alteracao, CancellationToken cancellationToken)
    {
        var resultado = await mcp.ChamarFerramentaAsync(chamada.Nome, JsonDocument.Parse(chamada.Argumentos).RootElement, cancellationToken);
        if (resultado.Erro)
        {
            LogRecusada(logger, chamada.Nome, resultado.Texto);
            throw new RegraDeNegocioException($"O SolidCraft não conseguiu {EdicaoNoArquivo.Descricao(alteracao)}. Confira o modelo e tente de novo.");
        }
    }
}

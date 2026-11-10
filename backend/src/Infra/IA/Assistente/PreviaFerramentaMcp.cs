using System.Security.Cryptography;
using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Infra.IA.Assistente.Mcp;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// Prévia pelo próprio servidor MCP da ferramenta: abre o arquivo e exporta em SVG, sem IA. Fica em
/// memória pelo conteúdo do arquivo: a página da versão pede de novo a cada visita.
/// </summary>
internal sealed partial class PreviaFerramentaMcp(IEnumerable<IFerramentaNaIa> registradas, IMemoryCache cache, ILogger<PreviaFerramentaMcp> logger)
    : IPreviaFerramenta
{
    private static readonly MemoryCacheEntryOptions Validade = new() { SlidingExpiration = TimeSpan.FromHours(1) };

    private readonly Dictionary<string, DefinicaoDeFerramenta> _definicoes = registradas.Select(f => f.Definicao).ToDictionary(d => d.Codigo);

    public async Task<byte[]?> GerarAsync(string ferramenta, byte[] arquivo, CancellationToken cancellationToken)
    {
        if (!_definicoes.TryGetValue(ferramenta, out var definicao) || (definicao.ExportarPrevia is null && definicao.VistaParaOJoalheiro is null)
            || string.IsNullOrWhiteSpace(definicao.Executavel) || !File.Exists(definicao.Executavel))
        {
            return null;
        }

        var chave = $"previa:{ferramenta}:{Convert.ToHexString(SHA256.HashData(arquivo))}";
        if (cache.TryGetValue(chave, out byte[]? guardada))
        {
            return guardada;
        }

        var pasta = Directory.CreateTempSubdirectory("castwork-previa-");
        try
        {
            await using var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel, definicao.Argumentos, pasta.FullName, TimeSpan.FromSeconds(30), cancellationToken);
            var caminho = Path.Combine(pasta.FullName, "arquivo" + definicao.ExtensaoDoArquivo);
            await File.WriteAllBytesAsync(caminho, arquivo, cancellationToken);
            if (!await ChamarAsync(mcp, definicao.AbrirArquivo(caminho), cancellationToken))
            {
                return null;
            }

            byte[] previa;
            if (definicao.ExportarPrevia is not null)
            {
                var svg = Path.Combine(pasta.FullName, "previa.svg");
                if (!await ChamarAsync(mcp, definicao.ExportarPrevia(svg), cancellationToken) || !File.Exists(svg))
                {
                    return null;
                }

                previa = await File.ReadAllBytesAsync(svg, cancellationToken);
            }
            else
            {
                // Sem exportação vetorial (o 3D): a mesma vista em PNG que o joalheiro vê com a resposta da IA.
                var (nome, argumentos) = definicao.VistaParaOJoalheiro!.Value;
                var resultado = await mcp.ChamarFerramentaAsync(nome, JsonDocument.Parse(argumentos).RootElement, cancellationToken);
                if (resultado.Erro || resultado.Conteudo.FirstOrDefault(c => c.Imagem is not null)?.Imagem is not { } imagem)
                {
                    return null;
                }

                previa = imagem;
            }

            cache.Set(chave, previa, Validade);
            return previa;
        }
        catch (IOException ex)
        {
            LogFalha(logger, ex);
            return null;
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível gerar a prévia do arquivo da ferramenta.")]
    private static partial void LogFalha(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "O servidor MCP recusou {Ferramenta} ao gerar a prévia: {Detalhe}")]
    private static partial void LogRecusada(ILogger logger, string ferramenta, string detalhe);

    private async Task<bool> ChamarAsync(ClienteMcp mcp, (string Nome, string Argumentos) chamada, CancellationToken cancellationToken)
    {
        var resultado = await mcp.ChamarFerramentaAsync(chamada.Nome, JsonDocument.Parse(chamada.Argumentos).RootElement, cancellationToken);
        if (resultado.Erro)
        {
            LogRecusada(logger, chamada.Nome, resultado.Texto);
        }

        return !resultado.Erro;
    }
}

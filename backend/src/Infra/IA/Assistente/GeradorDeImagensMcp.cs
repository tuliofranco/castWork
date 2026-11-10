using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Infra.IA.Assistente.Mcp;

using Microsoft.Extensions.Logging;

namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// As imagens guardadas pelo próprio servidor MCP da ferramenta, sem IA: abre o arquivo uma vez e faz cada chamada de
/// <see cref="DefinicaoDeFerramenta.VistasGuardadas"/>. Uma vista que o programa recusa fica de fora (e vai para o log);
/// as outras seguem.
/// </summary>
internal sealed partial class GeradorDeImagensMcp(IEnumerable<IFerramentaNaIa> registradas, ILogger<GeradorDeImagensMcp> logger) : IGeradorDeImagens
{
    /// <summary>
    /// Por chamada: o render grande do modelo 3D é feito na CPU.
    /// </summary>
    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(90);

    private readonly Dictionary<string, DefinicaoDeFerramenta> _definicoes = registradas.Select(f => f.Definicao).ToDictionary(d => d.Codigo);

    public async Task<IReadOnlyList<ImagemGerada>> GerarAsync(string ferramenta, byte[] arquivo, CancellationToken cancellationToken)
    {
        if (!_definicoes.TryGetValue(ferramenta, out var definicao) || definicao.VistasGuardadas is not { Count: > 0 } vistas
            || string.IsNullOrWhiteSpace(definicao.Executavel) || !File.Exists(definicao.Executavel))
        {
            return [];
        }

        var pasta = Directory.CreateTempSubdirectory("castwork-imagens-");
        try
        {
            await using var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel, definicao.Argumentos, pasta.FullName, TempoLimite, cancellationToken);
            var caminho = Path.Combine(pasta.FullName, "arquivo" + definicao.ExtensaoDoArquivo);
            await File.WriteAllBytesAsync(caminho, arquivo, cancellationToken);
            var (abrir, argumentosDeAbrir) = definicao.AbrirArquivo(caminho);
            var aberto = await mcp.ChamarFerramentaAsync(abrir, JsonDocument.Parse(argumentosDeAbrir).RootElement, cancellationToken);
            if (aberto.Erro)
            {
                throw new InvalidOperationException($"{ferramenta}: o programa não abriu o arquivo: {aberto.Texto}");
            }

            var geradas = new List<ImagemGerada>();
            for (var i = 0; i < vistas.Count; i++)
            {
                var vista = vistas[i];
                var destino = Path.Combine(pasta.FullName, $"imagem-{i}{vista.Extensao}");
                var (nome, argumentos) = vista.Chamada(destino);
                var resultado = await mcp.ChamarFerramentaAsync(nome, JsonDocument.Parse(argumentos).RootElement, cancellationToken);
                var imagem = resultado.Erro ? null
                    : resultado.Conteudo.FirstOrDefault(c => c.Imagem is not null)?.Imagem
                    ?? (File.Exists(destino) ? await File.ReadAllBytesAsync(destino, cancellationToken) : null);
                if (imagem is { Length: > 0 })
                {
                    geradas.Add(new ImagemGerada(vista.Vista, imagem));
                }
                else
                {
                    LogVistaRecusada(logger, ferramenta, vista.Vista, nome, resultado.Texto);
                }
            }

            return geradas;
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Ferramenta}: a vista {Vista} não foi gerada ({Chamada}): {Detalhe}")]
    private static partial void LogVistaRecusada(ILogger logger, string ferramenta, string vista, string chamada, string detalhe);
}

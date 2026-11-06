using System.Diagnostics;
using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.Documentos.WordCraft;

/// <summary>
/// Gera o PDF do briefing com o WordCraft, sem janela (<c>wordcraft-cli run --script</c>), a partir do
/// roteiro de <see cref="RoteiroDoBriefing"/>.
/// </summary>
internal sealed partial class GeradorBriefingWordCraft(IOptions<WordCraftOptions> options, ILogger<GeradorBriefingWordCraft> logger)
    : IGeradorDocumentoBriefing
{
    /// <summary>
    /// Fontes da marca, lidas uma vez dos recursos do assembly.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<(string Nome, byte[] Conteudo)>> Fontes = new(() =>
    {
        var assembly = typeof(GeradorBriefingWordCraft).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("Fontes.", StringComparison.Ordinal))
            .Select(n =>
            {
                using var recurso = assembly.GetManifestResourceStream(n)!;
                using var memoria = new MemoryStream();
                recurso.CopyTo(memoria);
                return (n["Fontes.".Length..], memoria.ToArray());
            })
            .ToList();
    });

    private static readonly string[] Saidas = ["briefing.pdf"];

    private readonly WordCraftOptions _opcoes = options.Value;

    public async Task<byte[]> GerarPdfAsync(DadosDocumentoBriefing dados, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.Caminho) || !File.Exists(_opcoes.Caminho))
        {
            throw new ServicoIndisponivelException("O gerador de PDF não está configurado neste ateliê.");
        }

        // Pasta própria por geração: nada fica para trás e gerações simultâneas não se misturam.
        var pasta = Directory.CreateTempSubdirectory("castwork-briefing-");
        try
        {
            var script = Path.Combine(pasta.FullName, "job.json");
            await File.WriteAllTextAsync(script, JsonSerializer.Serialize(new { template = "blank", commands = RoteiroDoBriefing.Montar(dados, Fontes.Value), save = Saidas }), cancellationToken);

            using var processo = Process.Start(new ProcessStartInfo(_opcoes.Caminho, ["run", "--script", script])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = pasta.FullName,
            })!;
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(TimeSpan.FromSeconds(_opcoes.TempoLimiteEmSegundos));
            var erros = processo.StandardError.ReadToEndAsync(limite.Token);
            try
            {
                await processo.WaitForExitAsync(limite.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                processo.Kill(entireProcessTree: true);
                throw new ServicoIndisponivelException("O gerador de PDF demorou demais para responder.");
            }

            var pdf = Path.Combine(pasta.FullName, "briefing.pdf");
            if (processo.ExitCode != 0 || !File.Exists(pdf))
            {
                LogFalha(logger, processo.ExitCode, await erros);
                throw new ServicoIndisponivelException("Não foi possível gerar o PDF do briefing.");
            }

            return await File.ReadAllBytesAsync(pdf, cancellationToken);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "WordCraft saiu com código {Codigo}: {Erros}")]
    private static partial void LogFalha(ILogger logger, int codigo, string erros);
}

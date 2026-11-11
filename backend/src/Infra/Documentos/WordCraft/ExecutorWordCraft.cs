using System.Diagnostics;
using System.Text.Json;

using Castwork.Application.Common.Exceptions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.Documentos.WordCraft;

/// <summary>
/// Roda um roteiro no WordCraft, sem janela (<c>wordcraft-cli run --script</c>), e devolve o PDF. Usado pelos geradores
/// do briefing e da ficha técnica; cada geração tem a sua pasta temporária, apagada no fim.
/// </summary>
internal sealed partial class ExecutorWordCraft(IOptions<WordCraftOptions> options, ILogger<ExecutorWordCraft> logger)
{
    private const string Saida = "documento.pdf";

    private static readonly string[] Saidas = [Saida];

    private readonly WordCraftOptions _opcoes = options.Value;

    /// <summary>
    /// Roda o roteiro (<paramref name="comandos"/>, montado por um <see cref="RoteiroWordCraft"/>) e devolve o PDF; se o
    /// WordCraft não gerar o arquivo, <paramref name="falha"/> é a mensagem para o usuário.
    /// </summary>
    public async Task<byte[]> GerarPdfAsync(List<object> comandos, string falha, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.Caminho) || !File.Exists(_opcoes.Caminho))
        {
            throw new ServicoIndisponivelException("O gerador de PDF não está configurado neste ateliê.");
        }

        // Pasta própria por geração: nada fica para trás e gerações simultâneas não se misturam.
        var pasta = Directory.CreateTempSubdirectory("castwork-documento-");
        try
        {
            var script = Path.Combine(pasta.FullName, "job.json");
            await File.WriteAllTextAsync(script, JsonSerializer.Serialize(new { template = "blank", commands = comandos, save = Saidas }), cancellationToken);

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

            var pdf = Path.Combine(pasta.FullName, Saida);
            if (processo.ExitCode != 0 || !File.Exists(pdf))
            {
                LogFalha(logger, processo.ExitCode, await erros);
                throw new ServicoIndisponivelException(falha);
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

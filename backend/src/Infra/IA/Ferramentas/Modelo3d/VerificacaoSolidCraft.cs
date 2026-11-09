using System.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// Ao subir a API, confere se o <c>solidcraft-cli</c> configurado responde e registra a versão no log.
/// Não impede a subida: sem o SolidCraft, só o modelo 3D com IA fica indisponível.
/// </summary>
internal sealed partial class VerificacaoSolidCraft(IOptions<SolidCraftOptions> options, ILogger<VerificacaoSolidCraft> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var opcoes = options.Value;
        if (string.IsNullOrWhiteSpace(opcoes.Caminho) || !File.Exists(opcoes.Caminho))
        {
            LogNaoConfigurado(logger);
            return;
        }

        try
        {
            using var processo = Process.Start(new ProcessStartInfo(opcoes.Caminho, ["--version"]) { RedirectStandardOutput = true, RedirectStandardError = true })!;
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(TimeSpan.FromSeconds(opcoes.TempoLimiteEmSegundos));
            var saida = (await processo.StandardOutput.ReadToEndAsync(limite.Token)).Trim();
            await processo.WaitForExitAsync(limite.Token);
            if (processo.ExitCode == 0)
            {
                LogDisponivel(logger, saida);
            }
            else
            {
                LogFalha(logger, processo.ExitCode);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or System.ComponentModel.Win32Exception)
        {
            LogErro(logger, ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "SolidCraft disponível: {Versao}")]
    private static partial void LogDisponivel(ILogger logger, string versao);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SolidCraft não configurado (SolidCraft:Caminho): o modelo 3D com IA fica indisponível.")]
    private static partial void LogNaoConfigurado(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "O solidcraft-cli respondeu com código {Codigo}.")]
    private static partial void LogFalha(ILogger logger, int codigo);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível executar o solidcraft-cli.")]
    private static partial void LogErro(ILogger logger, Exception ex);
}

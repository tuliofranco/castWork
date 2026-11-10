using System.Threading.Channels;

using Castwork.Application.Abstractions;
using Castwork.Application.Assistente.Commands;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// A fila das imagens em memória: o arquivo entra ao ir para o banco e o <see cref="ImagensEmSegundoPlano"/> processa um
/// por vez. Some se a API reiniciar antes: o arquivo fica sem imagens e a tela usa a prévia sob demanda.
/// </summary>
internal sealed class FilaDeImagensEmMemoria : IFilaDeImagens
{
    private readonly Channel<long> _canal = Channel.CreateUnbounded<long>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<long> Leitor => _canal.Reader;

    public void Enfileirar(long arquivoId) => _canal.Writer.TryWrite(arquivoId);
}

/// <summary>
/// Gera as imagens de cada arquivo da fila (<see cref="GerarImagensDoArquivoCommand"/>), num escopo próprio, um por vez
/// (cada um abre o programa da ferramenta). Uma falha vai para o log e a fila segue.
/// </summary>
internal sealed partial class ImagensEmSegundoPlano(FilaDeImagensEmMemoria fila, IServiceScopeFactory escopos, ILogger<ImagensEmSegundoPlano> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var arquivoId in fila.Leitor.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var escopo = escopos.CreateScope();
                await escopo.ServiceProvider.GetRequiredService<ISender>().Send(new GerarImagensDoArquivoCommand { ArquivoId = arquivoId }, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // Qualquer falha de um arquivo não pode parar a fila.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogFalha(logger, arquivoId, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível gerar as imagens do arquivo {ArquivoId}.")]
    private static partial void LogFalha(ILogger logger, long arquivoId, Exception ex);
}

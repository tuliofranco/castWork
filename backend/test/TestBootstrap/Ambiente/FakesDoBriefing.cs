using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Entities.Ferramentas.Briefings;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Gerador de PDF de mentira: devolve bytes fixos e guarda os últimos dados recebidos para conferência.
/// </summary>
public sealed class GeradorDocumentoBriefingDeTeste : IGeradorDocumentoBriefing
{
    public static readonly byte[] Pdf = "%PDF-1.7 teste"u8.ToArray();

    public static DadosDocumentoBriefing? UltimosDados { get; private set; }

    public Task<byte[]> GerarPdfAsync(DadosDocumentoBriefing dados, CancellationToken cancellationToken)
    {
        UltimosDados = dados;
        return Task.FromResult(Pdf);
    }
}

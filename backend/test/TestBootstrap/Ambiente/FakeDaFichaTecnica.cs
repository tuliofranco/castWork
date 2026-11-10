using System.Text;

using Castwork.Application.Abstractions;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Gerador da ficha técnica de mentira: devolve um "PDF" com o texto que iria no documento (um campo por linha), para o
/// teste conferir o que entra e o que fica de fora sem o binário do WordCraft.
/// </summary>
public sealed class GeradorFichaTecnicaDeTeste : IGeradorFichaTecnica
{
    public Task<byte[]> GerarPdfAsync(DadosFichaTecnica dados, CancellationToken cancellationToken)
    {
        var linhas = new List<string?> { "%PDF-1.7 teste", "FICHA TÉCNICA", dados.Marca.Nome, dados.Nome, dados.Projeto, dados.Cliente, $"{dados.Data:dd/MM/yyyy}" };
        linhas.AddRange(dados.Especificacoes.Select(e => $"{e.Rotulo}: {e.Valor}"));
        linhas.AddRange((dados.Imagens ?? []).Select(i => $"Imagem: {i.Legenda} ({i.Png.Length} bytes)"));
        linhas.AddRange([dados.Descricao, dados.Valor is null ? null : $"Valor no projeto: {dados.Valor}", dados.Briefing]);
        return Task.FromResult(Encoding.UTF8.GetBytes(string.Join('\n', linhas.Where(l => l is not null))));
    }
}

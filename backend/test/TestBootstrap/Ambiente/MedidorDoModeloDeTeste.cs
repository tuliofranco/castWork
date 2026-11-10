using Castwork.Application.Ferramentas.Modelo3d;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Medidor de mentira (sem SolidCraft): devolve a <see cref="Medicao"/> que o teste montou (nula: falha ao medir) e conta
/// as chamadas, com o arquivo e os materiais recebidos. Zerado a cada teste.
/// </summary>
public sealed class MedidorDoModeloDeTeste : IMedidorDoModelo
{
    private readonly List<(long ArquivoId, IReadOnlyList<MaterialDoModelo> Materiais)> _chamadas = [];

    public MedicaoDoModelo? Medicao { get; set; }

    public IReadOnlyList<(long ArquivoId, IReadOnlyList<MaterialDoModelo> Materiais)> Chamadas => _chamadas;

    public async Task<MedicaoDoModelo?> MedirAsync(ArquivoParaMedir arquivo, IReadOnlyList<MaterialDoModelo> materiais, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arquivo);

        // Lê o conteúdo como o medidor real faria, para o teste valer também a leitura do arquivo.
        _ = await arquivo.LerConteudo(cancellationToken);
        lock (_chamadas)
        {
            _chamadas.Add((arquivo.Id, materiais));
        }

        return Medicao;
    }

    public void Reiniciar()
    {
        Medicao = null;
        lock (_chamadas)
        {
            _chamadas.Clear();
        }
    }
}

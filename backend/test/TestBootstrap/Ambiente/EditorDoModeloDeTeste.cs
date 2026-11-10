using System.Text;

using Castwork.Application.Ferramentas.Modelo3d;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Editor de mentira (sem SolidCraft): devolve o arquivo recebido com a alteração anotada no fim (um conteúdo novo, como
/// o real) e guarda as chamadas. <see cref="Falha"/>: a exceção que ele lança no lugar (SolidCraft indisponível, recusa).
/// Zerado a cada teste.
/// </summary>
public sealed class EditorDoModeloDeTeste : IEditorDoModelo
{
    private readonly List<(byte[] Arquivo, IReadOnlyList<MaterialDoModelo> Materiais, AlteracaoDoModelo Alteracao)> _chamadas = [];

    public Exception? Falha { get; set; }

    public IReadOnlyList<(byte[] Arquivo, IReadOnlyList<MaterialDoModelo> Materiais, AlteracaoDoModelo Alteracao)> Chamadas => _chamadas;

    public Task<byte[]> AlterarAsync(byte[] arquivo, IReadOnlyList<MaterialDoModelo> materiais, AlteracaoDoModelo alteracao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arquivo);
        lock (_chamadas)
        {
            _chamadas.Add((arquivo, materiais, alteracao));
        }

        if (Falha is not null)
        {
            throw Falha;
        }

        return Task.FromResult<byte[]>([.. arquivo, .. Encoding.UTF8.GetBytes($"\n{alteracao}")]);
    }

    public void Reiniciar()
    {
        Falha = null;
        lock (_chamadas)
        {
            _chamadas.Clear();
        }
    }
}

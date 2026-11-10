namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// Altera o arquivo 3D sem IA, para a conferência de peso e materiais: garante nele os materiais do ateliê (os mesmos do
/// turno da IA), aplica a alteração e devolve o conteúdo novo, que vira uma nova versão do arquivo. Não cria nem apaga
/// corpos: só define o material e une corpos.
/// </summary>
public interface IEditorDoModelo
{
    /// <summary>
    /// O arquivo alterado. SolidCraft indisponível: <c>ServicoIndisponivelException</c>; alteração recusada pelo
    /// SolidCraft: <c>RegraDeNegocioException</c>.
    /// </summary>
    Task<byte[]> AlterarAsync(byte[] arquivo, IReadOnlyList<MaterialDoModelo> materiais, AlteracaoDoModelo alteracao, CancellationToken cancellationToken);
}

/// <summary>
/// O que a conferência muda no arquivo.
/// </summary>
public abstract record AlteracaoDoModelo;

/// <summary>
/// O material (pelo nome no arquivo: <c>Castwork liga 2</c>, "Pedra", "Apoio (não conta)") de cada corpo.
/// </summary>
public sealed record DefinicaoDeMateriais(IReadOnlyList<MaterialDoCorpo> Corpos) : AlteracaoDoModelo;

public sealed record MaterialDoCorpo(string Corpo, string Material);

/// <summary>
/// Une os <see cref="Outros"/> ao <see cref="Alvo"/>: viram um corpo só, com o nome e o material do alvo.
/// </summary>
public sealed record UniaoDeCorpos(string Alvo, IReadOnlyList<string> Outros) : AlteracaoDoModelo;

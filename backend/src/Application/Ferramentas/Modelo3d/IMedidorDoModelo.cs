namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// Mede o arquivo 3D atual da versão, sem IA: garante nele os materiais do ateliê (os mesmos do turno da IA) e devolve
/// os corpos e as sobreposições entre eles. Guarda a medição pelo arquivo (id) e pelos materiais: só mede de novo quando
/// um dos dois muda.
/// </summary>
public interface IMedidorDoModelo
{
    /// <summary>
    /// Nulo quando não foi possível medir (SolidCraft indisponível, arquivo com problema): o preço sai incompleto.
    /// </summary>
    Task<MedicaoDoModelo?> MedirAsync(ArquivoParaMedir arquivo, IReadOnlyList<MaterialDoModelo> materiais, CancellationToken cancellationToken);
}

/// <summary>
/// O arquivo pelo id (cada alteração é um arquivo novo no histórico) e como ler o conteúdo, que só é lido quando a
/// medição não está guardada.
/// </summary>
public sealed record ArquivoParaMedir(long Id, Func<CancellationToken, Task<byte[]>> LerConteudo);

/// <summary>
/// O que o SolidCraft mediu: todos os corpos e todas as sobreposições (o preço separa as de metal).
/// </summary>
public sealed record MedicaoDoModelo(IReadOnlyList<CorpoMedido> Corpos, IReadOnlyList<SobreposicaoMedida> Sobreposicoes);

/// <summary>
/// Um corpo: nome, material (nulo sem nenhum), volume em mm³ e massa em g (nulos em corpo aberto) e se é fechado.
/// </summary>
public sealed record CorpoMedido(string Nome, string? Material, decimal? VolumeMm3, decimal? MassaGramas, bool Fechado);

/// <summary>
/// Dois corpos que se sobrepõem e o volume em comum (mm³).
/// </summary>
public sealed record SobreposicaoMedida(string CorpoA, string CorpoB, decimal VolumeMm3);

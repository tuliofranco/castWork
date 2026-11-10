using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.ValueObjects;
using Castwork.DomainService.Precificacao;

namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// O peso do metal pelo modelo 3D da versão (o arquivo atual) ou de uma peça do projeto (o arquivo final): acha o arquivo, manda medir
/// (<see cref="IMedidorDoModelo"/>, sem IA) e classifica cada corpo pelo material: liga do ateliê (<c>Castwork liga {id}</c>),
/// "Pedra", "Apoio (não conta)" ou sem material (o "Default" do SolidCraft e qualquer material que não é do ateliê).
/// </summary>
public static class PesoDoModelo
{
    /// <summary>
    /// Nulo quando a versão não tem 3D (nenhum arquivo, ou o atual está em branco): vale o peso manual. Se não foi
    /// possível medir, <see cref="ModeloMedido.NaoMedido"/>.
    /// </summary>
    public static async Task<ModeloMedido?> MedirAsync(IUnitOfWork unitOfWork, IMedidorDoModelo medidor, long versaoId, CancellationToken cancellationToken)
    {
        var atual = await MedirArquivoAtualAsync(unitOfWork, medidor, versaoId, cancellationToken);
        return atual is null ? null : atual.Medicao is null ? ModeloMedido.NaoMedido : Classificar(atual.Medicao);
    }

    /// <summary>
    /// O arquivo atual do Modelo 3D da versão medido, com os materiais do ateliê usados na medição. Nulo quando a versão
    /// não tem 3D; <see cref="ModeloAtual.Medicao"/> nula quando não foi possível medir.
    /// </summary>
    public static async Task<ModeloAtual?> MedirArquivoAtualAsync(IUnitOfWork unitOfWork, IMedidorDoModelo medidor, long versaoId, CancellationToken cancellationToken)
    {
        var conversaId = await unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.VersaoId == versaoId && c.Ferramenta == Modelo3dNoAssistente.Codigo)
            .Select(c => (long?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return await MedirConversaAsync(unitOfWork, medidor, conversaId, cancellationToken);
    }

    /// <summary>
    /// O arquivo final do Modelo 3D de uma peça do projeto medido (a mesma medição guardada da versão). Nulo quando a peça
    /// não tem 3D final; <see cref="ModeloAtual.Medicao"/> nula quando não foi possível medir.
    /// </summary>
    public static async Task<ModeloAtual?> MedirArquivoFinalDaPecaAsync(IUnitOfWork unitOfWork, IMedidorDoModelo medidor, long pecaDoProjetoId, CancellationToken cancellationToken)
    {
        var finalId = await unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.PecaDoProjetoId == pecaDoProjetoId && c.Ferramenta == Modelo3dNoAssistente.Codigo)
            .Select(c => c.ArquivoFinalId)
            .FirstOrDefaultAsync(cancellationToken);
        if (finalId is null)
        {
            return null;
        }

        var final = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.Id == finalId)
            .Select(a => new { a.Id, EmBranco = a.Conteudo.Length == 0 })
            .FirstOrDefaultAsync(cancellationToken);
        return final is null || final.EmBranco ? null : await MedirArquivoAsync(unitOfWork, medidor, final.Id, cancellationToken);
    }

    /// <summary>
    /// O peso da peça do projeto pelo modelo dela, todo na liga da peça: a soma do volume dos corpos que contam (os de
    /// metal e os sem material; "Pedra" e "Apoio" ficam de fora, e o corpo aberto também, porque não tem volume confiável)
    /// × a densidade da liga. O material de cada corpo no arquivo não muda o peso: a peça é de uma liga só.
    /// </summary>
    public static Peso PesoNaLiga(MedicaoDoModelo medicao, decimal densidadeGramasPorCm3)
    {
        ArgumentNullException.ThrowIfNull(medicao);
        var volumeMm3 = medicao.Corpos
            .Where(c => c.Fechado && c.VolumeMm3 is > 0 && Uso(c.Material) is UsoDoCorpo.Metal or UsoDoCorpo.SemMaterial)
            .Sum(c => c.VolumeMm3!.Value);
        return Peso.EmGramas(volumeMm3 * densidadeGramasPorCm3 / 1000m);
    }

    public static ModeloMedido Classificar(MedicaoDoModelo medicao)
    {
        ArgumentNullException.ThrowIfNull(medicao);
        return new ModeloMedido(
            Medido: true,
            [.. medicao.Corpos.Select(c => new CorpoDoModelo(
                c.Nome,
                Uso(c.Material),
                MateriaisDoModelo.LigaDoMaterial(c.Material),
                c.MassaGramas is >= 0 ? c.MassaGramas : null,
                c.Fechado))],
            [.. medicao.Sobreposicoes.Select(s => new SobreposicaoDeCorpos(s.CorpoA, s.CorpoB, s.VolumeMm3))]);
    }

    /// <summary>
    /// Para que serve o corpo, pelo nome do material no arquivo.
    /// </summary>
    public static UsoDoCorpo Uso(string? material)
        => MateriaisDoModelo.LigaDoMaterial(material) is not null ? UsoDoCorpo.Metal
            : material == MateriaisDoModelo.Pedra ? UsoDoCorpo.Pedra
            : material == MateriaisDoModelo.Apoio ? UsoDoCorpo.Apoio
            : UsoDoCorpo.SemMaterial;

    /// <summary>
    /// O atual é o mais recente não arquivado (como na tela).
    /// </summary>
    private static async Task<ModeloAtual?> MedirConversaAsync(IUnitOfWork unitOfWork, IMedidorDoModelo medidor, long? conversaId, CancellationToken cancellationToken)
    {
        if (conversaId is null)
        {
            return null;
        }

        var atual = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.ConversaId == conversaId && a.ArquivadoEm == null)
            .OrderByDescending(a => a.Id)
            .Select(a => new { a.Id, EmBranco = a.Conteudo.Length == 0 })
            .FirstOrDefaultAsync(cancellationToken);
        return atual is null || atual.EmBranco ? null : await MedirArquivoAsync(unitOfWork, medidor, atual.Id, cancellationToken);
    }

    /// <summary>
    /// Mede um arquivo 3D; o conteúdo só é lido se a medição não estiver guardada.
    /// </summary>
    private static async Task<ModeloAtual> MedirArquivoAsync(IUnitOfWork unitOfWork, IMedidorDoModelo medidor, long arquivoId, CancellationToken cancellationToken)
    {
        var materiais = await MateriaisDoModelo.CarregarAsync(unitOfWork, cancellationToken);
        var arquivo = new ArquivoParaMedir(
            arquivoId,
            ct => unitOfWork.GetRepository<ArquivoFerramenta>().FindBy(a => a.Id == arquivoId).Select(a => a.Conteudo).SingleAsync(ct));
        return new ModeloAtual(arquivoId, materiais, await medidor.MedirAsync(arquivo, materiais, cancellationToken));
    }
}

/// <summary>
/// O arquivo 3D atual da versão (<see cref="ArquivoId"/>), os materiais do ateliê e a medição dele; nula quando não foi
/// possível medir.
/// </summary>
public sealed record ModeloAtual(long ArquivoId, IReadOnlyList<MaterialDoModelo> Materiais, MedicaoDoModelo? Medicao);

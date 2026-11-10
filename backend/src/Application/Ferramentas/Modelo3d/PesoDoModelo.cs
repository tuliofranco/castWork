using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Assistente;
using Castwork.DomainService.Precificacao;

namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// O peso do metal pelo modelo 3D da versão: acha o arquivo atual do Modelo 3D, manda medir (<see cref="IMedidorDoModelo"/>,
/// sem IA) e classifica cada corpo pelo material: liga do ateliê (<c>Castwork liga {id}</c>), "Pedra", "Apoio (não conta)"
/// ou sem material (o "Default" do SolidCraft e qualquer material que não é do ateliê).
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
        if (conversaId is null)
        {
            return null;
        }

        // O atual é o mais recente não arquivado (como na tela); o conteúdo só é lido se a medição não estiver guardada.
        var atual = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.ConversaId == conversaId && a.ArquivadoEm == null)
            .OrderByDescending(a => a.Id)
            .Select(a => new { a.Id, EmBranco = a.Conteudo.Length == 0 })
            .FirstOrDefaultAsync(cancellationToken);
        if (atual is null || atual.EmBranco)
        {
            return null;
        }

        var materiais = await MateriaisDoModelo.CarregarAsync(unitOfWork, cancellationToken);
        var arquivo = new ArquivoParaMedir(
            atual.Id,
            ct => unitOfWork.GetRepository<ArquivoFerramenta>().FindBy(a => a.Id == atual.Id).Select(a => a.Conteudo).SingleAsync(ct));
        return new ModeloAtual(atual.Id, materiais, await medidor.MedirAsync(arquivo, materiais, cancellationToken));
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
}

/// <summary>
/// O arquivo 3D atual da versão (<see cref="ArquivoId"/>), os materiais do ateliê e a medição dele; nula quando não foi
/// possível medir.
/// </summary>
public sealed record ModeloAtual(long ArquivoId, IReadOnlyList<MaterialDoModelo> Materiais, MedicaoDoModelo? Medicao);

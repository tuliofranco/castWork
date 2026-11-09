using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;
using Castwork.Domain.Entities.Ferramentas.Briefings;

namespace Castwork.Application.Ferramentas.Briefings;

/// <summary>
/// O PDF do briefing como anexo "Do projeto": só o último gerado (os anteriores ficam no briefing).
/// </summary>
public sealed class BriefingComoAnexo(IUnitOfWork unitOfWork) : IFonteDeAnexos
{
    public string Origem => BriefingNoAssistente.Codigo;

    public async Task<IReadOnlyList<AnexoDoProjetoDto>> ListarAsync(long projetoId, CancellationToken cancellationToken)
    {
        var ultimo = await Documentos(projetoId)
            .OrderByDescending(d => d.Id)
            .Select(d => new AnexoDoProjetoDto(BriefingNoAssistente.Codigo, d.Id, d.NomeArquivo, BriefingNoAssistente.Codigo, null, d.GeradoEm, d.Conteudo.Length))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        return ultimo is null ? [] : [ultimo];
    }

    public async Task<AnexoCarregado?> CarregarAsync(long projetoId, long id, CancellationToken cancellationToken)
        => await Documentos(projetoId)
            .Where(d => d.Id == id)
            .Select(d => new AnexoCarregado(d.NomeArquivo, "Briefing do projeto", BriefingNoAssistente.Codigo, ".pdf", d.Conteudo))
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

    private IQueryable<DocumentoBriefing> Documentos(long projetoId)
    {
        var briefings = unitOfWork.GetRepository<Briefing>().FindBy(b => b.ProjetoId == projetoId).Select(b => b.Id);
        return unitOfWork.GetRepository<DocumentoBriefing>().FindBy(d => briefings.Contains(d.BriefingId));
    }
}

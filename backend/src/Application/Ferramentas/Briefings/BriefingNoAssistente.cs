using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;

namespace Castwork.Application.Ferramentas.Briefings;

/// <summary>
/// O briefing no Assistente: ferramenta do projeto que escreve o documento. A IA recebe o estado do briefing e
/// grava os campos e o PDF pelas ferramentas locais (<see cref="FerramentasDoBriefing"/>); abre a conversa
/// sozinha, resumindo o formulário.
/// </summary>
public sealed class BriefingNoAssistente(IUnitOfWork unitOfWork, IGeradorDocumentoBriefing gerador, TimeProvider relogio) : IFerramentaComIa
{
    public const string Codigo = "briefing";

    string IFerramentaComIa.Codigo => Codigo;

    public bool DoProjeto => true;

    public bool AbreSozinha => true;

    public async Task<PreparoDoTurno> PrepararAsync(long projetoId, long versaoId, CancellationToken cancellationToken)
    {
        var briefing = await unitOfWork.ObterDoProjetoAsync(projetoId, cancellationToken);
        var nomes = await ContextoDoBriefing.NomesAsync(unitOfWork, projetoId, cancellationToken);
        var contexto = ContextoDoBriefing.ParaIa(briefing, nomes, DateOnly.FromDateTime(relogio.GetLocalNow().DateTime));
        return new PreparoDoTurno(contexto, new FerramentasDoBriefing(unitOfWork, gerador, relogio, briefing));
    }
}

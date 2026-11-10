using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;

namespace Castwork.Application.Ferramentas.Desenho2d;

/// <summary>
/// O desenho 2D no Assistente: ferramenta da versão e de cada peça do projeto. A IA recebe projeto, versão, composição e
/// briefing (ou, na peça, a peça e o briefing) e desenha no DXF dela pelo MCP do CADCraft (definido na Infra); não tem
/// ferramentas locais.
/// </summary>
public sealed class Desenho2dNoAssistente(IUnitOfWork unitOfWork) : IFerramentaComIa
{
    public const string Codigo = "desenho-2d";

    string IFerramentaComIa.Codigo => Codigo;

    public bool DoProjeto => false;

    public bool DaPeca => true;

    public bool AbreSozinha => false;

    public async Task<PreparoDoTurno> PrepararAsync(IEscopoDaFerramenta escopo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(escopo);
        return new(escopo.PecaDoProjetoId != 0
            ? await ContextoDoDesenho2d.MontarDaPecaAsync(unitOfWork, escopo.ProjetoId, escopo.PecaDoProjetoId, cancellationToken)
            : await ContextoDoDesenho2d.MontarAsync(unitOfWork, escopo.ProjetoId, escopo.VersaoId, cancellationToken));
    }
}

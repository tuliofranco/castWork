using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;

namespace Castwork.Application.Ferramentas.Desenho2d;

/// <summary>
/// O desenho 2D no Assistente: ferramenta da versão. A IA recebe projeto, versão, composição e briefing e
/// desenha no DXF da versão pelo MCP do CADCraft (definido na Infra); não tem ferramentas locais.
/// </summary>
public sealed class Desenho2dNoAssistente(IUnitOfWork unitOfWork) : IFerramentaComIa
{
    public const string Codigo = "desenho-2d";

    string IFerramentaComIa.Codigo => Codigo;

    public bool DoProjeto => false;

    public bool AbreSozinha => false;

    public async Task<PreparoDoTurno> PrepararAsync(long projetoId, long versaoId, CancellationToken cancellationToken)
        => new(await ContextoDoDesenho2d.MontarAsync(unitOfWork, projetoId, versaoId, cancellationToken));
}

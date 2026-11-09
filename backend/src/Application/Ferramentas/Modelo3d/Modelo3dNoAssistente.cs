using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;

namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// O modelo 3D no Assistente: ferramenta da versão. A IA recebe projeto, versão, composição e briefing e
/// modela no arquivo da versão pelo MCP do SolidCraft (definido na Infra); não tem ferramentas locais.
/// </summary>
public sealed class Modelo3dNoAssistente(IUnitOfWork unitOfWork) : IFerramentaComIa
{
    public const string Codigo = "modelo-3d";

    string IFerramentaComIa.Codigo => Codigo;

    public bool DoProjeto => false;

    public bool AbreSozinha => false;

    public async Task<PreparoDoTurno> PrepararAsync(long projetoId, long versaoId, CancellationToken cancellationToken)
        => new(await ContextoDoModelo3d.MontarAsync(unitOfWork, projetoId, versaoId, cancellationToken));
}

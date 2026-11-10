using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;

namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// O modelo 3D no Assistente: ferramenta da versão. A IA recebe projeto, versão, composição, briefing e os materiais do
/// ateliê, e modela no arquivo da versão pelo MCP do SolidCraft (definido na Infra); não tem ferramentas locais. Ao abrir o
/// arquivo, a Infra garante nele os mesmos materiais (o preparo do arquivo, <see cref="MateriaisDoModelo.ParaPreparo"/>).
/// </summary>
public sealed class Modelo3dNoAssistente(IUnitOfWork unitOfWork) : IFerramentaComIa
{
    public const string Codigo = "modelo-3d";

    string IFerramentaComIa.Codigo => Codigo;

    public bool DoProjeto => false;

    public bool AbreSozinha => false;

    public async Task<PreparoDoTurno> PrepararAsync(long projetoId, long versaoId, CancellationToken cancellationToken)
    {
        var materiais = await MateriaisDoModelo.CarregarAsync(unitOfWork, cancellationToken);
        var contexto = await ContextoDoModelo3d.MontarAsync(unitOfWork, projetoId, versaoId, materiais, cancellationToken);
        return new PreparoDoTurno(contexto, PreparoDoArquivo: MateriaisDoModelo.ParaPreparo(materiais));
    }
}

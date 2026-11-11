using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;

namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// O modelo 3D no Assistente: ferramenta da versão e de cada peça do projeto. A IA recebe projeto, versão e composição (ou,
/// na peça, a peça), o briefing e os materiais do ateliê, e modela no arquivo dela pelo MCP do SolidCraft (definido na
/// Infra); não tem ferramentas locais. Ao abrir o
/// arquivo, a Infra garante nele os mesmos materiais (o preparo do arquivo, <see cref="MateriaisDoModelo.ParaPreparo"/>).
/// Ao marcar o arquivo final da peça, os corpos recebem o nome dela (<see cref="NomesDosCorpos"/>).
/// </summary>
public sealed class Modelo3dNoAssistente(IUnitOfWork unitOfWork, IMedidorDoModelo medidor, IEditorDoModelo editor) : IFerramentaComIa, IArquivoFinalDaFerramenta
{
    public const string Codigo = "modelo-3d";

    string IFerramentaComIa.Codigo => Codigo;

    public bool DoProjeto => false;

    public bool DaPeca => true;

    public bool AbreSozinha => false;

    public async Task<PreparoDoTurno> PrepararAsync(IEscopoDaFerramenta escopo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(escopo);
        var materiais = await MateriaisDoModelo.CarregarAsync(unitOfWork, cancellationToken);
        var contexto = escopo.PecaDoProjetoId != 0
            ? await ContextoDoModelo3d.MontarDaPecaAsync(unitOfWork, escopo.ProjetoId, escopo.PecaDoProjetoId, materiais, cancellationToken)
            : await ContextoDoModelo3d.MontarAsync(unitOfWork, escopo.ProjetoId, escopo.VersaoId, materiais, cancellationToken);
        return new PreparoDoTurno(contexto, PreparoDoArquivo: MateriaisDoModelo.ParaPreparo(materiais));
    }

    /// <summary>
    /// Mede o arquivo (medição guardada pelo Id) para saber os nomes dos corpos e, se algum não tem o nome da peça, renomeia
    /// pelo editor. Sem medição (o SolidCraft não respondeu), marca como está.
    /// </summary>
    public async Task<byte[]?> AjustarAsync(long arquivoId, byte[] conteudo, string nomeDaPeca, CancellationToken cancellationToken)
    {
        var materiais = await MateriaisDoModelo.CarregarAsync(unitOfWork, cancellationToken);
        var medicao = await medidor.MedirAsync(new ArquivoParaMedir(arquivoId, _ => Task.FromResult(conteudo)), materiais, cancellationToken);
        if (medicao is null)
        {
            return null;
        }

        var renomeacoes = NomesDosCorpos.Renomeacoes([.. medicao.Corpos.Select(c => c.Nome)], nomeDaPeca);
        return renomeacoes.Count == 0 ? null : await editor.AlterarAsync(conteudo, materiais, new RenomeacaoDeCorpos(renomeacoes), cancellationToken);
    }
}

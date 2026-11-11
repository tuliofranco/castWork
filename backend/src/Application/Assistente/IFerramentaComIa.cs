using Castwork.Application.Abstractions;

namespace Castwork.Application.Assistente;

/// <summary>
/// O que uma ferramenta registra no Assistente para conversar com a IA: o código (o da rota), o escopo e, a
/// cada turno, o contexto que a IA recebe e as ferramentas locais dela. O Assistente não conhece nenhuma
/// ferramenta pelo nome: só por este contrato. Cada ferramenta implementa o seu em <c>Ferramentas/&lt;Nome&gt;</c>.
/// </summary>
public interface IFerramentaComIa
{
    /// <summary>
    /// Código da ferramenta na rota (<c>desenho-2d</c>, <c>briefing</c>).
    /// </summary>
    string Codigo { get; }

    /// <summary>
    /// Ferramenta do projeto (vale para todas as versões e só se edita pela rota do projeto) ou da versão.
    /// </summary>
    bool DoProjeto { get; }

    /// <summary>
    /// Ferramenta da versão que também abre em cada peça do projeto (o desenho 2D e o modelo 3D da peça), com conversa e
    /// arquivo próprios da peça.
    /// </summary>
    bool DaPeca { get; }

    /// <summary>
    /// Com o chat vazio, a IA pode abrir a conversa sozinha (sem fala do joalheiro).
    /// </summary>
    bool AbreSozinha { get; }

    /// <summary>
    /// O contexto do turno no escopo da conversa: o projeto, a versão ou a peça do projeto.
    /// </summary>
    Task<PreparoDoTurno> PrepararAsync(IEscopoDaFerramenta escopo, CancellationToken cancellationToken);
}

/// <summary>
/// O contexto (JSON) que a IA recebe no turno e as ferramentas locais dela (nulo quando só usa o MCP).
/// <see cref="PreparoDoArquivo"/>: o que o arquivo de trabalho precisa ter ao ser aberto (JSON que só a ferramenta
/// entende, ex.: os materiais do modelo 3D); nulo quando não há.
/// </summary>
public sealed record PreparoDoTurno(string Contexto, IFerramentasLocais? Locais = null, string? PreparoDoArquivo = null);

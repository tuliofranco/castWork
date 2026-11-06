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
    /// Com o chat vazio, a IA pode abrir a conversa sozinha (sem fala do joalheiro).
    /// </summary>
    bool AbreSozinha { get; }

    Task<PreparoDoTurno> PrepararAsync(long projetoId, long versaoId, CancellationToken cancellationToken);
}

/// <summary>
/// O contexto (JSON) que a IA recebe no turno e as ferramentas locais dela (nulo quando só usa o MCP).
/// </summary>
public sealed record PreparoDoTurno(string Contexto, IFerramentasLocais? Locais = null);

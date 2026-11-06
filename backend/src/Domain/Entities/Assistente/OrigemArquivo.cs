namespace Castwork.Domain.Entities.Assistente;

/// <summary>
/// Quem gravou a versão do arquivo.
/// </summary>
public enum OrigemArquivo
{
    /// <summary>A IA, pelo MCP da ferramenta.</summary>
    Ia = 1,

    /// <summary>O joalheiro, editando na tela e salvando.</summary>
    Joalheiro = 2,

    /// <summary>Um Desfazer: cópia de uma versão anterior.</summary>
    Desfazer = 3,

    /// <summary>O joalheiro restaurou uma versão do histórico: cópia dela.</summary>
    Restauracao = 4,
}

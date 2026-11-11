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

    /// <summary>A conferência de peso e materiais da versão: o joalheiro definiu o material ou uniu corpos, e o sistema aplicou no arquivo.</summary>
    Conferencia = 5,

    /// <summary>Cópia do desenho da peça da biblioteca, trazida ao importar a peça para o projeto.</summary>
    Biblioteca = 6,

    /// <summary>"Montar a partir das peças": o modelo 3D da versão com uma cópia do modelo de cada peça da composição.</summary>
    Montagem = 7,

    /// <summary>Marcar como final: o modelo 3D da peça com os corpos renomeados com o nome da peça.</summary>
    NomesDaPeca = 8,
}

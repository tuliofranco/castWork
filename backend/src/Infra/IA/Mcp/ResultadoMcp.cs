namespace Castwork.Infra.IA.Mcp;

/// <summary>
/// O retorno de uma chamada: textos e imagens, e se o servidor considerou erro.
/// </summary>
internal sealed record ResultadoMcp(IReadOnlyList<ConteudoMcp> Conteudo, bool Erro)
{
    public string Texto => string.Join("\n", Conteudo.Where(c => c.Texto is not null).Select(c => c.Texto));
}

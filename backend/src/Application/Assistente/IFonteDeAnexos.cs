namespace Castwork.Application.Assistente;

/// <summary>
/// De onde vêm os arquivos do projeto que o joalheiro anexa numa conversa ("Do projeto"). O Assistente
/// traz a sua (os arquivos das ferramentas); uma ferramenta com arquivos próprios, como o PDF do briefing,
/// registra a dela. Registradas por varredura, como <see cref="IFerramentaComIa"/>.
/// </summary>
public interface IFonteDeAnexos
{
    /// <summary>
    /// Código da origem no anexo (<c>arquivo</c>, <c>briefing</c>).
    /// </summary>
    string Origem { get; }

    Task<IReadOnlyList<AnexoDoProjetoDto>> ListarAsync(long projetoId, CancellationToken cancellationToken);

    /// <summary>
    /// O anexo com o conteúdo, para a IA; nulo se o id não é desta origem no projeto.
    /// </summary>
    Task<AnexoCarregado?> CarregarAsync(long projetoId, long id, CancellationToken cancellationToken);
}

/// <summary>
/// Um arquivo que pode ser anexado: <see cref="Ferramenta"/> é o código da ferramenta que o gravou e
/// <see cref="Versao"/> o número da versão (nulo no que é do projeto, como o briefing).
/// </summary>
public sealed record AnexoDoProjetoDto(
    string Origem,
    long Id,
    string Nome,
    string Ferramenta,
    int? Versao,
    DateTimeOffset CriadoEm,
    int Tamanho);

/// <summary>
/// O anexo com o conteúdo. <see cref="Descricao"/> diz à IA de onde ele é (<c>desenho-2d · versão 2</c>). <see cref="Extensao"/> nula: a da ferramenta que o gravou (a Infra sabe qual).
/// </summary>
public sealed record AnexoCarregado(string Nome, string Descricao, string Ferramenta, string? Extensao, byte[] Conteudo);

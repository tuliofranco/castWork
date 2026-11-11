namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Situação da peça do projeto em relação à biblioteca. Gravado como texto no banco.
/// </summary>
public enum StatusPecaDoProjeto
{
    /// <summary>
    /// Nasceu no projeto (ou teve os dados alterados) e ainda não está na biblioteca. Impede publicar a versão que a usa.
    /// </summary>
    Rascunho = 1,

    /// <summary>
    /// Está na biblioteca: foi publicada a partir do projeto ou importada de lá. Vinculada à peça da biblioteca.
    /// </summary>
    Publicada = 2,
}

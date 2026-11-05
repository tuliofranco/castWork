namespace Castwork.Domain.Entities.Projetos;

public enum StatusVersao
{
    /// <summary>
    /// Composição livre para editar.
    /// </summary>
    EmDesenvolvimento = 1,

    /// <summary>
    /// Composição congelada: o que foi mostrado ou aprovado não muda. Para alterar, duplique em nova versão.
    /// </summary>
    Publicada = 2,
}

namespace Castwork.Application.Assistente;

/// <summary>
/// A ferramenta da peça que ajusta o arquivo ao marcá-lo como o final da peça (o modelo 3D: os corpos com o nome da peça).
/// Opcional: a ferramenta que não implementa marca o arquivo como está.
/// </summary>
public interface IArquivoFinalDaFerramenta
{
    /// <summary>
    /// O conteúdo ajustado para ser o final da peça <paramref name="nomeDaPeca"/>, que vira uma nova versão do arquivo; nulo
    /// quando o arquivo já está como deve (ou não foi possível conferir).
    /// </summary>
    Task<byte[]?> AjustarAsync(long arquivoId, byte[] conteudo, string nomeDaPeca, CancellationToken cancellationToken);
}

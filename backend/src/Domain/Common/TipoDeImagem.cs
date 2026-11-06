namespace Castwork.Domain.Common;

/// <summary>
/// Reconhece imagens pelos primeiros bytes, não pela extensão ou pelo tipo informado pelo navegador.
/// </summary>
public static class TipoDeImagem
{
    public static string? Detectar(byte[] conteudo) => conteudo switch
    {
        [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
        [0xFF, 0xD8, 0xFF, ..] => "image/jpeg",
        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => "image/webp",
        _ => null,
    };
}

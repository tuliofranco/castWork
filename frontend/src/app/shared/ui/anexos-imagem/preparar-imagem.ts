/** Uma imagem pronta para ir à IA: base64 (sem o prefixo data:) e uma URL local para a miniatura. */
export interface ImagemAnexada {
  readonly base64: string;
  readonly url: string;
}

/**
 * Lado maior com que a imagem vai para a IA. Acima disso a API reduz de qualquer forma: mandar menor
 * economiza upload no tablet, tokens e o limite de 5 MB por imagem.
 */
const LADO_MAXIMO = 1568;
const QUALIDADE_JPEG = 0.85;

/** HEIC/HEIF, o formato das fotos do iPhone. Alguns navegadores entregam o arquivo sem `type`, por isso a extensão também conta. */
export function ehHeic(arquivo: File): boolean {
  return /^image\/hei[cf]/.test(arquivo.type) || /\.hei[cf]$/i.test(arquivo.name);
}

/**
 * Reduz a foto (câmera do tablet chega a 12 MP) e converte para JPEG. EXIF de rotação é aplicado pelo
 * `createImageBitmap`, então a foto não chega deitada.
 */
export async function prepararImagem(arquivo: File): Promise<ImagemAnexada> {
  const bitmap = await abrir(arquivo);
  const escala = Math.min(1, LADO_MAXIMO / Math.max(bitmap.width, bitmap.height));
  const largura = Math.round(bitmap.width * escala);
  const altura = Math.round(bitmap.height * escala);

  const canvas = document.createElement('canvas');
  canvas.width = largura;
  canvas.height = altura;
  const contexto = canvas.getContext('2d');
  if (!contexto) throw new Error('Canvas 2D indisponível');
  // Fundo branco: PNG transparente vira JPEG sem fundo preto.
  contexto.fillStyle = '#fff';
  contexto.fillRect(0, 0, largura, altura);
  contexto.drawImage(bitmap, 0, 0, largura, altura);
  bitmap.close();

  const url = canvas.toDataURL('image/jpeg', QUALIDADE_JPEG);
  return { base64: url.slice(url.indexOf(',') + 1), url };
}

/**
 * O Safari abre HEIC sozinho; Chrome, Brave e Firefox não. Nesses, a foto é decodificada pelo `heic-to` (libheif em
 * WebAssembly, ~3 MB), carregado só quando chega um HEIC, para não pesar no bundle.
 */
async function abrir(arquivo: File): Promise<ImageBitmap> {
  try {
    return await createImageBitmap(arquivo, { imageOrientation: 'from-image' });
  } catch (erro) {
    if (!ehHeic(arquivo)) throw erro;
    const { heicTo } = await import('heic-to');
    return heicTo({ blob: arquivo, type: 'bitmap' });
  }
}

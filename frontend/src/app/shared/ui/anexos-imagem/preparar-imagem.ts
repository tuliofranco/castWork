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

/**
 * Reduz a foto (câmera do tablet chega a 12 MP) e converte para JPEG. EXIF de rotação é aplicado pelo
 * `createImageBitmap`, então a foto não chega deitada.
 */
export async function prepararImagem(arquivo: File): Promise<ImagemAnexada> {
  const bitmap = await createImageBitmap(arquivo, { imageOrientation: 'from-image' });
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

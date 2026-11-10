import { ehHeic } from './preparar-imagem';

describe('ehHeic', () => {
  const arquivo = (nome: string, tipo: string) => new File([new Uint8Array([0])], nome, { type: tipo });

  it('deve reconhecer HEIC e HEIF pelo tipo ou pela extensão', () => {
    expect(ehHeic(arquivo('IMG_8248.HEIC', 'image/heic'))).toBe(true);
    expect(ehHeic(arquivo('foto.heif', 'image/heif'))).toBe(true);
    expect(ehHeic(arquivo('IMG_8248.HEIC', ''))).toBe(true);
    expect(ehHeic(arquivo('foto', 'image/heic-sequence'))).toBe(true);
  });

  it('não deve tratar outros formatos como HEIC', () => {
    expect(ehHeic(arquivo('foto.jpg', 'image/jpeg'))).toBe(false);
    expect(ehHeic(arquivo('foto.png', 'image/png'))).toBe(false);
    expect(ehHeic(arquivo('heic.txt', 'text/plain'))).toBe(false);
  });
});

import { emBase64 } from './em-base64';

describe('emBase64', () => {
  it('deve converter o conteúdo, inclusive além de um pedaço', async () => {
    expect(await emBase64(new Blob(['Castwork']))).toBe(btoa('Castwork'));

    const grande = 'a'.repeat(0x8000 + 10);
    expect(await emBase64(new Blob([grande]))).toBe(btoa(grande));
  });
});

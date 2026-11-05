import { formatarTeor, rotuloTipoPeca } from './tipo-peca';

describe('tipo-peca', () => {
  it.each([
    [750, '750 (18k)'],
    [585, '585 (14k)'],
    [925, '925 (22.2k)'],
    [null, '—'],
  ])('formatarTeor(%s) → %s', (milesimos, esperado) => {
    expect(formatarTeor(milesimos)).toBe(esperado);
  });

  it('deve traduzir o tipo para o rótulo da tela', () => {
    expect(rotuloTipoPeca('FECHO')).toBe('Fecho');
  });
});

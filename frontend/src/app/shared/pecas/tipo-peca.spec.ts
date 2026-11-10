import { formatarTeor, rotuloTipoPeca } from './tipo-peca';

describe('tipo-peca', () => {
  it.each([
    [750, 'OURO', '750 (18k)'],
    [585, 'OURO', '585 (14k)'],
    [916, 'OURO', '916 (22k)'],
    [925, 'PRATA', '925'],
    [750, null, '750'],
    [null, 'OURO', '—'],
  ] as const)('formatarTeor(%s, %s) → %s', (milesimos, metal, esperado) => {
    expect(formatarTeor(milesimos, metal)).toBe(esperado);
  });

  it('deve traduzir o tipo para o rótulo da tela', () => {
    expect(rotuloTipoPeca('FECHO')).toBe('Fecho');
  });
});

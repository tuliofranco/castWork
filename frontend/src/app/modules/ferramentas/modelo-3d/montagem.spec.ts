import { resumoDaMontagem } from './montagem';

describe('resumoDaMontagem', () => {
  it('conta as unidades e as peças trazidas', () => {
    expect(
      resumoDaMontagem({
        arquivoId: 2,
        arquivoAnteriorId: 1,
        pecas: [
          { nome: 'Elo oval', quantidade: 3 },
          { nome: 'Pingente', quantidade: 1 },
        ],
        pecasSemModelo: [],
      }),
    ).toBe('4 unidades de 2 peças trazidas para o modelo.');
  });

  it('no singular, e lista as peças sem modelo 3D', () => {
    expect(resumoDaMontagem({ arquivoId: 2, arquivoAnteriorId: null, pecas: [{ nome: 'Elo oval', quantidade: 1 }], pecasSemModelo: ['Fecho', 'Turmalina'] })).toBe(
      '1 unidade de 1 peça trazida para o modelo. Sem modelo 3D final: Fecho, Turmalina.',
    );
  });
});

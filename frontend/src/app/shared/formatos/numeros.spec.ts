import { moeda, numero } from './numeros';

describe('formatos de número', () => {
  // O espaço entre "R$" e o valor é o não separável do Intl.
  const semEspacos = (texto: string) => texto.replace(/\s/g, ' ');

  it('deve formatar moeda em reais', () => {
    expect(semEspacos(moeda(7850))).toBe('R$ 7.850,00');
    expect(semEspacos(moeda(0.5))).toBe('R$ 0,50');
  });

  it('deve formatar número com vírgula e até duas casas', () => {
    expect(numero(1.8)).toBe('1,8');
    expect(numero(2)).toBe('2');
    expect(numero(1.236)).toBe('1,24');
  });
});

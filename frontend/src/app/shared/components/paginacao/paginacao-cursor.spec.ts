import { PaginacaoCursor } from './paginacao-cursor';

describe('PaginacaoCursor', () => {
  it('deve avançar e voltar pelos cursores visitados', () => {
    const paginacao = new PaginacaoCursor();

    paginacao.proxima('c1');
    paginacao.proxima('c2');
    const naTerceira = { apos: paginacao.apos(), numero: paginacao.numero() };
    paginacao.anterior();

    expect(naTerceira).toEqual({ apos: 'c2', numero: 3 });
    expect({ apos: paginacao.apos(), numero: paginacao.numero(), temAnterior: paginacao.temAnterior() }).toEqual({
      apos: 'c1',
      numero: 2,
      temAnterior: true,
    });
  });

  it('deve voltar para a primeira página ao reiniciar', () => {
    const paginacao = new PaginacaoCursor();
    paginacao.proxima('c1');

    paginacao.reiniciar();

    expect({ apos: paginacao.apos(), numero: paginacao.numero(), temAnterior: paginacao.temAnterior() }).toEqual({
      apos: null,
      numero: 1,
      temAnterior: false,
    });
  });

  it('deve ignorar o mesmo cursor repetido (duplo clique em próxima)', () => {
    const paginacao = new PaginacaoCursor();

    paginacao.proxima('c1');
    paginacao.proxima('c1');

    expect({ apos: paginacao.apos(), numero: paginacao.numero() }).toEqual({ apos: 'c1', numero: 2 });
  });
});

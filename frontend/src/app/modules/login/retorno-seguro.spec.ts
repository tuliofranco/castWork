import { retornoSeguro } from './retorno-seguro';

describe('retornoSeguro', () => {
  it.each([
    ['/pecas?status=producao', '/pecas?status=producao'],
    [undefined, '/'],
    [null, '/'],
    ['', '/'],
    ['https://site-falso.com', '/'],
    ['//site-falso.com', '/'],
    ['/\\site-falso.com', '/'],
    ['javascript:alert(1)', '/'],
    ['/login?returnTo=/pecas', '/'],
  ])('retornoSeguro(%j) deve ser %j', (entrada, esperado) => {
    expect(retornoSeguro(entrada)).toBe(esperado);
  });
});

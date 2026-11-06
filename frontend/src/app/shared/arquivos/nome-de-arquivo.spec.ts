import { nomeDeArquivo } from './nome-de-arquivo';

describe('nomeDeArquivo', () => {
  it('deve tirar acentos e espaços e pôr a extensão', () => {
    expect(nomeDeArquivo('Visão 2d', 'dxf')).toBe('visao-2d.dxf');
    expect(nomeDeArquivo('  Anel Alegra — proposta  A ', '.dxf')).toBe('anel-alegra-proposta-a.dxf');
  });

  it('deve usar o padrão quando não sobra nome', () => {
    expect(nomeDeArquivo(null, 'dxf', 'desenho')).toBe('desenho.dxf');
    expect(nomeDeArquivo('###', 'dxf', 'desenho')).toBe('desenho.dxf');
  });
});

import { nomeDeArquivo, nomeDeArquivoDaPeca } from './nome-de-arquivo';

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

describe('nomeDeArquivoDaPeca', () => {
  it('deve manter o nome da peça como foi escrito', () => {
    expect(nomeDeArquivoDaPeca('Elo Infinito 2,5 cm', 'dxf')).toBe('Elo Infinito 2,5 cm.dxf');
    expect(nomeDeArquivoDaPeca('Elo Infinito 2,5 cm', 'png', 'Iso')).toBe('Elo Infinito 2,5 cm - Iso.png');
  });

  it('deve tirar os caracteres inválidos em nome de arquivo', () => {
    expect(nomeDeArquivoDaPeca(' Elo: 1/2 "fino"?  ', '.json')).toBe('Elo 12 fino.json');
    expect(nomeDeArquivoDaPeca('...', 'dxf')).toBe('peca.dxf');
  });
});

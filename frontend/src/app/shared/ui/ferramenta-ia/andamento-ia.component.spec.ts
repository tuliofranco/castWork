import { ANDAMENTO_VAZIO, aplicarEvento } from './andamento-ia.component';

describe('aplicarEvento', () => {
  it('deve somar os trechos de pensamento e de texto', () => {
    let andamento = ANDAMENTO_VAZIO();
    for (const evento of [
      { tipo: 'pensamento', texto: 'Vou ' },
      { tipo: 'pensamento', texto: 'desenhar.' },
      { tipo: 'texto', texto: 'Pronto.' },
    ]) {
      andamento = aplicarEvento(andamento, evento);
    }

    expect(andamento.pensamento).toBe('Vou desenhar.');
    expect(andamento.texto).toBe('Pronto.');
  });

  it('deve começar e terminar um passo, guardando o render', () => {
    let andamento = aplicarEvento(ANDAMENTO_VAZIO(), { tipo: 'passo', texto: 'Olhando o desenho', passo: 1, estado: 'andamento' });
    andamento = aplicarEvento(andamento, { tipo: 'passo', passo: 1, estado: 'feito', imagemPng: 'AAAA' });

    expect(andamento.passos).toEqual([{ numero: 1, descricao: 'Olhando o desenho', estado: 'feito', imagem: 'data:image/png;base64,AAAA' }]);
  });

  it('deve ignorar o fim', () => {
    const andamento = ANDAMENTO_VAZIO();

    expect(aplicarEvento(andamento, { tipo: 'fim' })).toBe(andamento);
  });
});

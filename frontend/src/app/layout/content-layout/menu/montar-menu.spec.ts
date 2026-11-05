import type { CastworkRoute } from '@core/models/rota-data.model';

import { montarMenu } from './montar-menu';

describe('montarMenu', () => {
  const rotas: CastworkRoute[] = [
    { path: '', redirectTo: 'pecas', pathMatch: 'full' },
    { path: 'pecas', data: { title: 'Peças', menu: true, order: 2, icon: 'gem', permissoes: ['pecas.ler'] } },
    { path: 'clientes', data: { title: 'Clientes', menu: true, order: 1, icon: 'people' } },
    { path: 'oculta', data: { title: 'Oculta', order: 0 } },
  ];

  it('deve incluir só as rotas com menu: true, ordenadas por order', () => {
    expect(montarMenu(rotas).map((item) => item.titulo)).toEqual(['Clientes', 'Peças']);
  });

  it('deve montar caminho absoluto e ícone', () => {
    expect(montarMenu(rotas)[0]).toEqual({ titulo: 'Clientes', caminho: '/clientes', icone: 'people', ordem: 1 });
  });

  it('deve esconder itens sem permissão', () => {
    const semPecas = (permissoes: readonly string[]) => !permissoes.includes('pecas.ler');
    expect(montarMenu(rotas, semPecas).map((item) => item.titulo)).toEqual(['Clientes']);
  });

  it('não deve consultar permissão de rota que não exige nenhuma', () => {
    const verificador = vi.fn(() => false);
    montarMenu([rotas[2]!], verificador);
    expect(verificador).not.toHaveBeenCalled();
  });

  it('deve usar valores padrão quando faltam icon e order', () => {
    const [item] = montarMenu([{ path: 'x', data: { menu: true } }]);
    expect(item).toEqual({ titulo: 'x', caminho: '/x', icone: 'circle', ordem: Number.MAX_SAFE_INTEGER });
  });
});

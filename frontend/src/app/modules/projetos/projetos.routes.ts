import type { CastworkRoute } from '@core/models/rota-data.model';

export const PROJETOS_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./projetos.component').then((m) => m.ProjetosComponent),
  },
  {
    path: 'novo',
    loadComponent: () => import('./novo-projeto/novo-projeto.component').then((m) => m.NovoProjetoComponent),
    data: { title: 'Novo projeto', permissoes: ['projetos.criar'] },
  },
  {
    path: ':id',
    loadComponent: () => import('./projeto-detalhe/projeto-detalhe.component').then((m) => m.ProjetoDetalheComponent),
    data: { title: 'Projeto' },
  },
];

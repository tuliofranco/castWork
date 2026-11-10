import type { CastworkRoute } from '@core/models/rota-data.model';

export const PECAS_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./pecas.component').then((m) => m.PecasComponent),
  },
  {
    path: 'nova',
    loadComponent: () => import('./nova-peca/nova-peca.component').then((m) => m.NovaPecaComponent),
    data: { title: 'Nova peça', permissoes: ['pecas.criar'] },
  },
  {
    path: ':id/editar',
    loadComponent: () => import('./nova-peca/nova-peca.component').then((m) => m.NovaPecaComponent),
    data: { title: 'Editar peça', permissoes: ['pecas.editar'] },
  },
];

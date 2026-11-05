import type { CastworkRoute } from '@core/models/rota-data.model';

export const PRECIFICACAO_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./precificacao.component').then((m) => m.PrecificacaoComponent),
  },
];

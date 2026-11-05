import type { CastworkRoute } from '@core/models/rota-data.model';

export const PECAS_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./pecas.component').then((m) => m.PecasComponent),
  },
];

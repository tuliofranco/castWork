import type { CastworkRoute } from '@core/models/rota-data.model';

export const ATELIE_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./atelie.component').then((m) => m.AtelieComponent),
  },
];

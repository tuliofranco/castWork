import type { CastworkRoute } from '@core/models/rota-data.model';

export const DOCUMENTOS_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./documentos.component').then((m) => m.DocumentosComponent),
  },
];

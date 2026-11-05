import type { CastworkRoute } from '@core/models/rota-data.model';

export const CLIENTES_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./clientes.component').then((m) => m.ClientesComponent),
  },
];

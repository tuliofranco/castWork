import type { CastworkRoute } from '@core/models/rota-data.model';

export const LOGIN_ROUTES: CastworkRoute[] = [
  { path: '', loadComponent: () => import('./login.component').then((m) => m.LoginComponent) },
];

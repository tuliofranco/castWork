import type { CastworkRoute } from '@core/models/rota-data.model';

export const PORTFOLIO_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./portfolio.component').then((m) => m.PortfolioComponent),
  },
];

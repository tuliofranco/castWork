import type { CastworkRoute } from '@core/models/rota-data.model';

/** A biblioteca é só de consulta: não se cria nem se edita peça aqui (ela chega pela publicação no projeto). */
export const PECAS_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./pecas.component').then((m) => m.PecasComponent),
  },
  {
    path: ':id',
    loadComponent: () => import('./peca-detalhe/peca-detalhe.component').then((m) => m.PecaDetalheComponent),
    data: { title: 'Peça' },
  },
];

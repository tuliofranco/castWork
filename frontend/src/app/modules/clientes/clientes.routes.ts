import type { CastworkRoute } from '@core/models/rota-data.model';

export const CLIENTES_ROUTES: CastworkRoute[] = [
  {
    path: '',
    loadComponent: () => import('./clientes.component').then((m) => m.ClientesComponent),
  },
  {
    path: 'novo',
    loadComponent: () => import('./novo-cliente/novo-cliente.component').then((m) => m.NovoClienteComponent),
    data: { title: 'Novo cliente', permissoes: ['clientes.criar'] },
  },
];

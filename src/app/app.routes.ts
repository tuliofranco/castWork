import type { CastworkRoute } from '@core/models/rota-data.model';
import { ContentLayoutComponent } from '@layout/content-layout/content-layout.component';
import { ErrorLayoutComponent } from '@layout/error-layout/error-layout.component';
import { PaginaErroComponent } from '@layout/error-layout/pagina-erro/pagina-erro.component';

/**
 * O `data` (title, menu, order, icon, permissoes) fica AQUI, no nível do lazy load:
 * o menu precisa dele antes de o chunk da feature ser baixado.
 */
export const routes: CastworkRoute[] = [
  {
    path: '',
    component: ContentLayoutComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'pecas' },
      {
        path: 'clientes',
        data: { title: 'Clientes', menu: true, order: 1, icon: 'people', permissoes: ['clientes.ler'] },
        loadChildren: () => import('@modules/clientes/clientes.routes').then((m) => m.CLIENTES_ROUTES),
      },
      {
        path: 'pecas',
        data: { title: 'Peças', menu: true, order: 2, icon: 'gem', permissoes: ['pecas.ler'] },
        loadChildren: () => import('@modules/pecas/pecas.routes').then((m) => m.PECAS_ROUTES),
      },
      {
        path: 'precificacao',
        data: { title: 'Precificação', menu: true, order: 3, icon: 'calculator', permissoes: ['precificacao.ler'] },
        loadChildren: () =>
          import('@modules/precificacao/precificacao.routes').then((m) => m.PRECIFICACAO_ROUTES),
      },
      {
        path: 'documentos',
        data: { title: 'Documentos', menu: true, order: 4, icon: 'file-earmark-pdf', permissoes: ['documentos.ler'] },
        loadChildren: () => import('@modules/documentos/documentos.routes').then((m) => m.DOCUMENTOS_ROUTES),
      },
      {
        path: 'portfolio',
        data: { title: 'Portfólio', menu: true, order: 5, icon: 'images', permissoes: ['portfolio.ler'] },
        loadChildren: () => import('@modules/portfolio/portfolio.routes').then((m) => m.PORTFOLIO_ROUTES),
      },
    ],
  },
  {
    path: '',
    component: ErrorLayoutComponent,
    children: [
      {
        path: '403',
        component: PaginaErroComponent,
        data: { title: 'Acesso negado', codigo: '403', mensagem: 'Você não tem permissão para acessar esta página.' },
      },
      {
        path: '**',
        component: PaginaErroComponent,
        data: { title: 'Página não encontrada', codigo: '404', mensagem: 'A página que você procura não existe.' },
      },
    ],
  },
];

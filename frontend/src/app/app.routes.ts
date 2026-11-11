import { sairDaFerramentaGuard } from '@core/guards/sair-da-ferramenta.guard';
import { inject } from '@angular/core';
import { PermissaoService } from '@core/services/permissao.service';
import { montarMenu } from '@layout/content-layout/menu/montar-menu';
import { authGuard } from '@core/guards/auth.guard';
import { convidadoGuard } from '@core/guards/convidado.guard';
import { permissionGuard } from '@core/guards/permission.guard';
import type { CastworkRoute } from '@core/models/rota-data.model';
import { ContentLayoutComponent } from '@layout/content-layout/content-layout.component';
import { ErrorLayoutComponent } from '@layout/error-layout/error-layout.component';
import { PaginaErroComponent } from '@layout/error-layout/pagina-erro/pagina-erro.component';

function inicioPermitido(): string {
  const permissao = inject(PermissaoService);
  const abas = routes.find((rota) => rota.component === ContentLayoutComponent)?.children ?? [];
  return montarMenu(abas, (permissoes) => permissao.temAlguma(permissoes))[0]?.caminho ?? '/403';
}

/**
 * O `data` (title, menu, order, icon, permissoes) fica AQUI, no nível do lazy load:
 * o menu precisa dele antes de o chunk da feature ser baixado.
 */
export const routes: CastworkRoute[] = [
  {
    path: 'login',
    canActivate: [convidadoGuard],
    data: { title: 'Entrar' },
    loadChildren: () => import('@modules/login/login.routes').then((m) => m.LOGIN_ROUTES),
  },
  {
    // Ferramentas com IA abrem em tela cheia, fora do layout com menu (modo de foco).
    path: 'projetos/:id/versoes/:versaoId/desenho-2d',
    canActivate: [authGuard, permissionGuard],
    data: { title: 'Desenho 2D', permissoes: ['projetos.ler'] },
    canDeactivate: [sairDaFerramentaGuard],
    loadComponent: () => import('@modules/ferramentas/desenho-2d/desenho-2d.component').then((m) => m.Desenho2dComponent),
  },
  {
    path: 'projetos/:id/versoes/:versaoId/modelo-3d',
    canActivate: [authGuard, permissionGuard],
    data: { title: 'Modelo 3D', permissoes: ['projetos.ler'] },
    canDeactivate: [sairDaFerramentaGuard],
    loadComponent: () => import('@modules/ferramentas/modelo-3d/modelo-3d.component').then((m) => m.Modelo3dComponent),
  },
  {
    // O desenho 2D e o modelo 3D de cada peça do projeto: as mesmas ferramentas, com conversa e arquivo só da peça.
    path: 'projetos/:id/pecas/:pecaId/desenho-2d',
    canActivate: [authGuard, permissionGuard],
    data: { title: 'Desenho 2D da peça', permissoes: ['projetos.ler'] },
    canDeactivate: [sairDaFerramentaGuard],
    loadComponent: () => import('@modules/ferramentas/desenho-2d/desenho-2d.component').then((m) => m.Desenho2dComponent),
  },
  {
    path: 'projetos/:id/pecas/:pecaId/modelo-3d',
    canActivate: [authGuard, permissionGuard],
    data: { title: 'Modelo 3D da peça', permissoes: ['projetos.ler'] },
    canDeactivate: [sairDaFerramentaGuard],
    loadComponent: () => import('@modules/ferramentas/modelo-3d/modelo-3d.component').then((m) => m.Modelo3dComponent),
  },
  {
    // O desenho atual de uma peça do projeto, só para ver (aberto pela composição da versão); editar é na página da peça.
    path: 'projetos/:id/pecas/:pecaId/desenhos/:ferramenta',
    canActivate: [authGuard, permissionGuard],
    data: { title: 'Desenho da peça', permissoes: ['projetos.ler'] },
    loadComponent: () => import('@modules/pecas/desenho-da-peca/desenho-da-peca.component').then((m) => m.DesenhoDaPecaComponent),
  },
  {
    // O desenho da peça da biblioteca, só para ver, também em tela cheia.
    path: 'pecas/:id/desenhos/:ferramenta',
    canActivate: [authGuard, permissionGuard],
    data: { title: 'Desenho da peça', permissoes: ['pecas.ler'] },
    loadComponent: () => import('@modules/pecas/desenho-da-peca/desenho-da-peca.component').then((m) => m.DesenhoDaPecaComponent),
  },
  {
    // O briefing é a ferramenta do projeto que escreve o documento: tela de foco, como o desenho 2D.
    path: 'projetos/:id/briefing',
    canActivate: [authGuard, permissionGuard],
    data: { title: 'Briefing', permissoes: ['projetos.ler'] },
    loadComponent: () => import('@modules/ferramentas/briefing/briefing.component').then((m) => m.BriefingComponent),
  },
  {
    path: '',
    component: ContentLayoutComponent,
    // authGuard roda uma vez ao entrar no layout; permissionGuard roda a cada rota filha.
    canActivate: [authGuard],
    canActivateChild: [permissionGuard],
    children: [
      // O início é a primeira aba do menu que a pessoa pode ver (a mesma conta do menu); sem nenhuma, 403.
      { path: '', pathMatch: 'full', redirectTo: () => inicioPermitido() },
      {
        path: 'projetos',
        data: { title: 'Projetos', menu: true, order: 1, icon: 'collection', permissoes: ['projetos.ler'] },
        loadChildren: () => import('@modules/projetos/projetos.routes').then((m) => m.PROJETOS_ROUTES),
      },
      {
        path: 'pecas',
        data: { title: 'Peças', menu: true, order: 2, icon: 'gem', permissoes: ['pecas.ler'] },
        loadChildren: () => import('@modules/pecas/pecas.routes').then((m) => m.PECAS_ROUTES),
      },
      {
        path: 'clientes',
        data: { title: 'Clientes', menu: true, order: 3, icon: 'people', permissoes: ['clientes.ler'] },
        loadChildren: () => import('@modules/clientes/clientes.routes').then((m) => m.CLIENTES_ROUTES),
      },
      {
        path: 'precificacao',
        data: { title: 'Precificação', menu: true, order: 4, icon: 'calculator', permissoes: ['precificacao.ler'] },
        loadChildren: () =>
          import('@modules/precificacao/precificacao.routes').then((m) => m.PRECIFICACAO_ROUTES),
      },
      {
        path: 'documentos',
        data: { title: 'Documentos', menu: true, order: 5, icon: 'file-earmark-pdf', permissoes: ['documentos.ler'] },
        loadChildren: () => import('@modules/documentos/documentos.routes').then((m) => m.DOCUMENTOS_ROUTES),
      },
      {
        // Fora do menu: abre pelo painel do usuário.
        path: 'atelie',
        data: { title: 'Ateliê', permissoes: ['atelie.configurar'] },
        loadChildren: () => import('@modules/atelie/atelie.routes').then((m) => m.ATELIE_ROUTES),
      },
      {
        path: 'portfolio',
        data: { title: 'Portfólio', menu: true, order: 6, icon: 'images', permissoes: ['portfolio.ler'] },
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

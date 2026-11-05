import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { CHAVE_ACCESS_TOKEN } from '@core/services/auth.service';
import { criarTokenMock } from '@data/auth/login/mocks/token-payload.mock';

import { ContentLayoutComponent } from './content-layout.component';

@Component({ template: '<p id="conteudo">conteúdo da feature</p>' })
class FeatureFalsaComponent {}

describe('ContentLayoutComponent', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    localStorage.setItem(CHAVE_ACCESS_TOKEN, criarTokenMock({ nome: 'Ana Ourives', atelieNome: 'Ateliê Sol', permissoes: ['pecas.ler'] }));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: '',
            component: ContentLayoutComponent,
            children: [
              { path: 'pecas', component: FeatureFalsaComponent, data: { title: 'Peças', menu: true, order: 2, icon: 'gem' } },
              { path: 'clientes', component: FeatureFalsaComponent, data: { title: 'Clientes', menu: true, order: 1, icon: 'people' } },
              { path: 'escondida', component: FeatureFalsaComponent, data: { title: 'Escondida' } },
              { path: 'restrita', component: FeatureFalsaComponent, data: { title: 'Restrita', menu: true, permissoes: ['documentos.ler'] } },
            ],
          },
        ]),
      ],
    });
    harness = await RouterTestingHarness.create('/pecas');
  });

  const links = () => Array.from(harness.routeNativeElement!.ownerDocument.querySelectorAll<HTMLAnchorElement>('cw-menu a'));

  it('deve gerar o menu a partir das rotas filhas, em ordem', () => {
    expect(links().map((a) => a.textContent?.trim())).toEqual(['Clientes', 'Peças']);
  });

  it('deve esconder do menu o item sem permissão', () => {
    expect(links().some((a) => a.textContent?.includes('Restrita'))).toBe(false);
  });

  it('deve mostrar usuário e ateliê da sessão no header', () => {
    const header = harness.routeNativeElement!.ownerDocument.querySelector('cw-header')!;
    expect(header.textContent).toContain('Ana Ourives');
    expect(header.textContent).toContain('Ateliê Sol');
  });

  it('deve marcar o item da rota ativa', () => {
    const ativo = links().find((a) => a.classList.contains('active'));
    expect(ativo?.getAttribute('href')).toBe('/pecas');
    expect(ativo?.getAttribute('aria-current')).toBe('page');
  });

  it('deve renderizar a feature no router-outlet', () => {
    expect(harness.routeNativeElement!.ownerDocument.querySelector('#conteudo')).not.toBeNull();
  });

  it('deve abrir o menu pelo header e fechar ao navegar', async () => {
    const doc = harness.routeNativeElement!.ownerDocument;
    const aside = doc.querySelector('aside')!;
    doc.querySelector<HTMLButtonElement>('cw-header button')!.click();
    await harness.fixture.whenStable();
    expect(aside.classList).toContain('aberto');

    links()[0]!.click();
    await harness.fixture.whenStable();
    expect(aside.classList).not.toContain('aberto');
  });

  afterEach(() => localStorage.clear());
});

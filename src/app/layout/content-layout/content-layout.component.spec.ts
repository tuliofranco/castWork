import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { ContentLayoutComponent } from './content-layout.component';

@Component({ template: '<p id="conteudo">conteúdo da feature</p>' })
class FeatureFalsaComponent {}

describe('ContentLayoutComponent', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
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
});

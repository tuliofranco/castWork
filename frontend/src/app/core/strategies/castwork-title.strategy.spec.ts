import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { provideRouter, TitleStrategy } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { CastworkTitleStrategy } from './castwork-title.strategy';

@Component({ template: '' })
class VazioComponent {}

describe('CastworkTitleStrategy', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'sem-titulo', component: VazioComponent },
          {
            path: 'pecas',
            data: { title: 'Peças' },
            children: [
              { path: '', component: VazioComponent },
              { path: 'detalhes', component: VazioComponent, data: { title: 'Detalhes da peça' } },
            ],
          },
        ]),
        { provide: TitleStrategy, useClass: CastworkTitleStrategy },
      ],
    });
  });

  it.each([
    ['/pecas', 'Peças | Castwork'],
    ['/pecas/detalhes', 'Detalhes da peça | Castwork'],
    ['/sem-titulo', 'Castwork'],
  ])('ao navegar para %s o título deve ser "%s"', async (url, esperado) => {
    // Cobre: sufixo, rota filha mais profunda vence a pai, e rota sem title.
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    expect(TestBed.inject(Title).getTitle()).toBe(esperado);
  });
});

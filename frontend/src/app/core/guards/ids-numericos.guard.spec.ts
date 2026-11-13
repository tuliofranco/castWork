import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { idsNumericos } from './ids-numericos.guard';

@Component({ template: '' })
class VazioComponent {}

describe('idsNumericos', () => {
  async function abrir(url: string): Promise<string | undefined> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'projetos/:id/versoes/:versaoId', canMatch: [idsNumericos], component: VazioComponent, data: { tela: 'versão' } },
          { path: 'pecas/:id/desenhos/:ferramenta', canMatch: [idsNumericos], component: VazioComponent, data: { tela: 'desenho' } },
          { path: '**', component: VazioComponent, data: { tela: '404' } },
        ]),
      ],
    });
    await RouterTestingHarness.create(url);
    return TestBed.inject(Router).routerState.snapshot.root.firstChild?.data['tela'] as string | undefined;
  }

  it.each([
    ['/projetos/1/versoes/20', 'versão'],
    ['/pecas/7/desenhos/desenho-2d', 'desenho'],
    ['/projetos/abc/versoes/20', '404'],
    ['/projetos/1/versoes/12x', '404'],
    ['/projetos/0/versoes/20', '404'],
    ['/pecas/-3/desenhos/desenho-2d', '404'],
  ])('%s deve abrir a tela "%s"', async (url, tela) => {
    expect(await abrir(url)).toBe(tela);
  });
});

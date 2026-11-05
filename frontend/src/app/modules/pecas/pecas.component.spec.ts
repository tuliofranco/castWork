import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { of } from 'rxjs';

import { PecasComponent } from './pecas.component';

// Mesmo locale do app (app.config): números com vírgula decimal.
registerLocaleData(localePt, 'pt-BR');

describe('PecasComponent', () => {
  it('deve listar a biblioteca com tipo, material, teor e peso', async () => {
    const getPecas = vi.fn().mockReturnValue(
      of({
        totalCount: 2,
        pageInfo: { hasNextPage: false, endCursor: null },
        nodes: [
          { id: 1, nome: 'Elo oval', tipo: 'ELO', material: 'Ouro amarelo', teor: 750, pesoGramas: 0.35, ativo: true },
          { id: 2, nome: 'Turmalina', tipo: 'PEDRA', material: null, teor: null, pesoGramas: null, ativo: false },
        ],
      }),
    );
    TestBed.configureTestingModule({
      imports: [PecasComponent],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: PecasService, useValue: { getPecas } },
        { provide: PermissaoService, useValue: { temAlguma: () => true } },
      ],
    });
    const fixture = TestBed.createComponent(PecasComponent);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    const linhas = [...el.querySelectorAll('tbody tr')].map((tr) => [...tr.querySelectorAll('td')].map((td) => td.textContent?.replace(/\s+/g, ' ').trim()));

    expect(linhas).toEqual([
      ['Elo oval', 'Elo', 'Ouro amarelo', '750 (18k)', '0,350 g'],
      ['Turmalina inativa', 'Pedra', '—', '—', '—'],
    ]);
    expect(el.textContent).toContain('Nova peça');
  });
});

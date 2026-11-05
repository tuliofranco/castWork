import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { of } from 'rxjs';

import { PecasComponent } from './pecas.component';

describe('PecasComponent', () => {
  const getPecas = vi.fn();

  async function abrir(podeCriar = true): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      imports: [PecasComponent],
      providers: [
        provideRouter([]),
        { provide: PecasService, useValue: { getPecas } },
        { provide: PermissaoService, useValue: { temAlguma: () => podeCriar } },
      ],
    });
    const fixture = TestBed.createComponent(PecasComponent);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => getPecas.mockReset());

  it('deve listar as peças na ordem recebida com etapa e cliente', async () => {
    getPecas.mockReturnValue(
      of({
        totalCount: 2,
        pageInfo: { hasNextPage: false, endCursor: null },
        nodes: [
          { id: 2, titulo: 'Brinco', status: 'ORCAMENTO', prazoEntrega: '2026-12-10', criadoEm: '2026-11-05T13:00:00Z', cliente: { id: 1, nome: 'Maria' } },
          { id: 1, titulo: 'Anel', status: 'BRIEFING', prazoEntrega: null, criadoEm: '2026-11-04T13:00:00Z', cliente: { id: 1, nome: 'Maria' } },
        ],
      }),
    );

    const el = await abrir();

    const linhas = [...el.querySelectorAll('tbody tr')].map((tr) => [...tr.querySelectorAll('td')].map((td) => td.textContent?.trim()));
    expect(linhas.map((l) => l.slice(0, 4))).toEqual([
      ['Brinco', 'Maria', 'Orçamento', '10/12/2026'],
      ['Anel', 'Maria', 'Briefing', '—'],
    ]);
    expect(getPecas).toHaveBeenCalledWith({ termo: null, apos: null, primeiros: 20 });
    expect(el.textContent).toContain('2 peça(s)');
  });

  it('deve mostrar estado vazio e esconder o botão sem permissão de criar', async () => {
    getPecas.mockReturnValue(of({ totalCount: 0, pageInfo: { hasNextPage: false, endCursor: null }, nodes: [] }));

    const el = await abrir(false);

    expect(el.textContent).toContain('Nenhuma peça cadastrada ainda.');
    expect(el.textContent).not.toContain('Nova peça');
  });
});

import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { of } from 'rxjs';

import { ProjetosComponent } from './projetos.component';

describe('ProjetosComponent', () => {
  it('deve listar os projetos com cliente e a versão mais recente', async () => {
    const getProjetos = vi.fn().mockReturnValue(
      of({
        totalCount: 2,
        pageInfo: { hasNextPage: false, endCursor: null },
        nodes: [
          { id: 2, nome: 'Pulseira autoral', cliente: null, versoes: [{ id: 5, numero: 1, status: 'EM_DESENVOLVIMENTO' }] },
          {
            id: 1,
            nome: 'Colar Raízes',
            cliente: { id: 3, nome: 'Maria Silva' },
            versoes: [
              { id: 4, numero: 2, status: 'EM_DESENVOLVIMENTO' },
              { id: 3, numero: 1, status: 'PUBLICADA' },
            ],
          },
        ],
      }),
    );
    TestBed.configureTestingModule({
      imports: [ProjetosComponent],
      providers: [
        provideRouter([]),
        { provide: ProjetosService, useValue: { getProjetos } },
        { provide: PermissaoService, useValue: { temAlguma: () => true } },
      ],
    });
    const fixture = TestBed.createComponent(ProjetosComponent);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    const itens = [...el.querySelectorAll('.list-group-item')].map((a) =>
      [...a.querySelectorAll('.fw-semibold, .small, .badge')].map((parte) => parte.textContent?.trim()).join(' | '),
    );

    expect(itens).toEqual([
      'Pulseira autoral | Sem cliente | v1 · 1 versão(ões) | Em desenvolvimento',
      'Colar Raízes | Maria Silva | v2 · 2 versão(ões) | Em desenvolvimento',
    ]);
    expect(el.querySelector('.list-group-item')!.getAttribute('href')).toBe('/2');
  });
});

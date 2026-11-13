import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { of, throwError } from 'rxjs';

import { ProjetosComponent } from './projetos.component';

describe('ProjetosComponent', () => {
  it('deve listar os projetos com cliente e a versão mais recente, ignorando as arquivadas', async () => {
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
              { id: 9, numero: 3, status: 'ARQUIVADA' },
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

    const itens = [...el.querySelectorAll('a.cw-linha')].map((a) =>
      [...a.querySelectorAll('.nome, .meta, cw-selo')].map((parte) => parte.textContent?.replace(/\s+/g, ' ').trim()).join(' | '),
    );

    expect(itens).toEqual([
      'Pulseira autoral | Projeto autoral · 1 versão | Em desenvolvimento',
      'Colar Raízes | Maria Silva · 2 versões | Em desenvolvimento',
    ]);
    expect(el.querySelector('a.cw-linha')!.getAttribute('href')).toBe('/2');
    expect(el.textContent).toContain('2 projetos');
  });

  it('deve mostrar o erro com Tentar de novo quando a lista não carrega', async () => {
    const pagina = { totalCount: 1, pageInfo: { hasNextPage: false, endCursor: null }, nodes: [{ id: 2, nome: 'Pulseira autoral', cliente: null, versoes: [] }] };
    const getProjetos = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('API fora')))
      .mockReturnValue(of(pagina));
    TestBed.configureTestingModule({
      imports: [ProjetosComponent],
      providers: [provideRouter([]), { provide: ProjetosService, useValue: { getProjetos } }, { provide: PermissaoService, useValue: { temAlguma: () => true } }],
    });
    const fixture = TestBed.createComponent(ProjetosComponent);
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    const tentar = [...el.querySelectorAll<HTMLButtonElement>('[role=alert] button')].find((b) => b.textContent?.trim() === 'Tentar de novo');
    expect(el.querySelector('[role=alert]')!.textContent).toContain('Não foi possível carregar os projetos.');
    expect(el.querySelector('.cw-linha')).toBeNull();

    tentar!.click();
    await fixture.whenStable();

    expect(getProjetos).toHaveBeenCalledTimes(2);
    expect(el.querySelector('[role=alert]')).toBeNull();
    expect(el.querySelectorAll('.cw-linha').length).toBe(1);
  });
});

import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { of, throwError } from 'rxjs';

import { ClientesComponent } from './clientes.component';

describe('ClientesComponent', () => {
  it('deve listar clientes com documento, contato e quantidade de projetos', async () => {
    const getClientes = vi.fn().mockReturnValue(
      of({
        totalCount: 2,
        pageInfo: { hasNextPage: true, endCursor: 'c1' },
        nodes: [
          { id: 1, nome: 'Ana', documento: '529.982.247-25', email: 'ana@x.com', telefone: '(41) 99999-0000', ativo: true, projetos: [{ id: 1 }, { id: 2 }] },
          { id: 2, nome: 'Bia', documento: null, email: null, telefone: null, ativo: false, projetos: [] },
        ],
      }),
    );
    TestBed.configureTestingModule({
      imports: [ClientesComponent],
      providers: [
        provideRouter([]),
        { provide: ClientesService, useValue: { getClientes } },
        { provide: PermissaoService, useValue: { temAlguma: () => true } },
      ],
    });
    const fixture = TestBed.createComponent(ClientesComponent);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    const linhas = [...el.querySelectorAll('.cw-linha')].map((linha) =>
      [...linha.querySelectorAll('cw-iniciais, .nome, .meta, cw-selo')].map((parte) => parte.textContent?.replace(/\s+/g, ' ').trim()),
    );

    expect(linhas).toEqual([
      ['A', 'Ana', '529.982.247-25 · (41) 99999-0000 · ana@x.com', '2 projetos'],
      ['B', 'Bia', 'Sem contato cadastrado', 'Inativo', '0 projetos'],
    ]);
    expect([...el.querySelectorAll<HTMLButtonElement>('cw-paginacao button')].map((b) => b.disabled)).toEqual([true, false]);
  });

  it('deve mostrar o erro com Tentar de novo quando a lista não carrega', async () => {
    const ana = { id: 1, nome: 'Ana', documento: null, email: null, telefone: null, ativo: true, projetos: [] };
    const getClientes = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('API fora')))
      .mockReturnValue(of({ totalCount: 1, pageInfo: { hasNextPage: false, endCursor: null }, nodes: [ana] }));
    TestBed.configureTestingModule({
      imports: [ClientesComponent],
      providers: [provideRouter([]), { provide: ClientesService, useValue: { getClientes } }, { provide: PermissaoService, useValue: { temAlguma: () => true } }],
    });
    const fixture = TestBed.createComponent(ClientesComponent);
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    const tentar = [...el.querySelectorAll<HTMLButtonElement>('[role=alert] button')].find((b) => b.textContent?.trim() === 'Tentar de novo');
    expect(el.querySelector('[role=alert]')!.textContent).toContain('Não foi possível carregar');
    expect(el.querySelector('.cw-linha')).toBeNull();

    tentar!.click();
    await fixture.whenStable();

    expect(getClientes).toHaveBeenCalledTimes(2);
    expect(el.querySelector('[role=alert]')).toBeNull();
    expect(el.querySelectorAll('.cw-linha').length).toBe(1);
  });
});

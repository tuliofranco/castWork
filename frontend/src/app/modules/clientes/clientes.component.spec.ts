import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { of } from 'rxjs';

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

    const linhas = [...el.querySelectorAll('tbody tr')].map((tr) => [...tr.querySelectorAll('td')].map((td) => td.textContent?.replace(/\s+/g, ' ').trim()));

    expect(linhas).toEqual([
      ['Ana', '529.982.247-25', '(41) 99999-0000ana@x.com', '2'],
      ['Bia inativo', '—', '', '0'],
    ]);
    expect(el.querySelector<HTMLButtonElement>('button:last-of-type')!.disabled).toBe(false);
  });
});

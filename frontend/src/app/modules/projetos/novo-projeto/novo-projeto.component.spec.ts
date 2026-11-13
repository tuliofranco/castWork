import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { NovoProjetoComponent } from './novo-projeto.component';

describe('NovoProjetoComponent', () => {
  const getClientesOpcoes = vi.fn(() => of([{ id: 3, nome: 'Maria Silva' }]));

  async function abrir(permissoes: readonly string[]) {
    getClientesOpcoes.mockClear();
    TestBed.configureTestingModule({
      imports: [NovoProjetoComponent],
      providers: [
        provideRouter([]),
        { provide: ProjetosService, useValue: {} },
        { provide: ClientesService, useValue: { getClientesOpcoes } },
        { provide: PermissaoService, useValue: { temAlguma: (pedidas: readonly string[]) => pedidas.some((p) => permissoes.includes(p)) } },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(NovoProjetoComponent);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    return { el, cliente: el.querySelector<HTMLSelectElement>('#clienteId')! };
  }

  const opcoes = (select: HTMLSelectElement) => [...select.options].map((o) => o.textContent?.trim());

  it('deve oferecer os clientes a quem pode vê-los', async () => {
    const { cliente } = await abrir(['projetos.criar', 'clientes.ler']);

    expect(opcoes(cliente)).toEqual(['Sem cliente', 'Maria Silva']);
    expect(cliente.disabled).toBe(false);
  });

  it('deve abrir sem pedir os clientes a quem não pode vê-los', async () => {
    const { el, cliente } = await abrir(['projetos.criar']);

    expect(getClientesOpcoes).not.toHaveBeenCalled();
    expect(opcoes(cliente)).toEqual(['Sem cliente']);
    expect(cliente.disabled).toBe(true);
    expect(el.textContent).toContain('Seu acesso não inclui a lista de clientes');
    expect(el.querySelector('#nome')).not.toBeNull();
  });
});

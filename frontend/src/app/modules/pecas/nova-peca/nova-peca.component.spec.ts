import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { ToastService } from '@shared/services/toast.service';
import { of, throwError } from 'rxjs';

import { NovaPecaComponent } from './nova-peca.component';

describe('NovaPecaComponent', () => {
  const postPeca = vi.fn();
  const sucesso = vi.fn();

  async function abrir() {
    TestBed.configureTestingModule({
      imports: [NovaPecaComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        { provide: PecasService, useValue: { postPeca } },
        { provide: ClientesService, useValue: { getClientesOpcoes: () => of([{ id: 7, nome: 'Maria' }]) } },
        { provide: ToastService, useValue: { sucesso } },
      ],
    });
    const fixture = TestBed.createComponent(NovaPecaComponent);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const preencher = async (clienteIndice: number, titulo: string) => {
      const select = el.querySelector<HTMLSelectElement>('#clienteId')!;
      select.selectedIndex = clienteIndice;
      select.dispatchEvent(new Event('change'));
      const campo = el.querySelector<HTMLInputElement>('#titulo')!;
      campo.value = titulo;
      campo.dispatchEvent(new Event('input'));
      el.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };
    return { el, preencher };
  }

  beforeEach(() => {
    postPeca.mockReset();
    sucesso.mockReset();
  });

  it('não deve enviar sem cliente e título', async () => {
    const { el, preencher } = await abrir();

    await preencher(0, '');

    expect(postPeca).not.toHaveBeenCalled();
    expect(el.textContent).toContain('Selecione o cliente.');
    expect(el.textContent).toContain('Informe o título.');
  });

  it('deve criar a peça e voltar para a listagem', async () => {
    postPeca.mockReturnValue(of({ id: 42 }));
    const { preencher } = await abrir();

    await preencher(1, 'Aliança');

    expect(postPeca).toHaveBeenCalledWith({ clienteId: 7, titulo: 'Aliança', descricao: null, prazoEntrega: null });
    expect(sucesso).toHaveBeenCalledWith('Peça criada em briefing.');
    expect(TestBed.inject(Router).url).toBe('/pecas');
  });

  it('deve mostrar no campo o erro de validação devolvido pela API', async () => {
    postPeca.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { errors: { titulo: ['Título já usado em outra peça.'] } } })),
    );
    const { el, preencher } = await abrir();

    await preencher(1, 'Aliança');

    expect(el.querySelector('#titulo')!.classList).toContain('is-invalid');
    expect(el.textContent).toContain('Título já usado em outra peça.');
  });
});

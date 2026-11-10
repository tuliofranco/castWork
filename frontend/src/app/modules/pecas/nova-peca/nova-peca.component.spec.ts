import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import type { PecaDetalheDto } from '@data/pecas/pecas/dto/peca.dto';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { NovaPecaComponent } from './nova-peca.component';

describe('NovaPecaComponent', () => {
  const turmalina: PecaDetalheDto = {
    id: 7,
    nome: 'Turmalina 4 mm',
    tipo: 'PEDRA',
    material: 'Turmalina',
    teor: null,
    pesoGramas: null,
    descricao: null,
    ativo: true,
    custo: 85.5,
  };

  const servico = {
    getPeca: vi.fn(() => of(turmalina)),
    postPeca: vi.fn(() => of({ id: 9 })),
    putPeca: vi.fn(() => of(undefined)),
  };

  async function abrir(opcoes: { readonly id?: string; readonly veCusto?: boolean } = {}) {
    Object.values(servico).forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [NovaPecaComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        { provide: PecasService, useValue: servico },
        { provide: PermissaoService, useValue: { temAlguma: () => opcoes.veCusto ?? true } },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(NovaPecaComponent);
    if (opcoes.id) fixture.componentRef.setInput('id', opcoes.id);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable() };
  }

  function digitar(el: HTMLElement, id: string, valor: string): void {
    const campo = el.querySelector<HTMLInputElement>(`#${id}`)!;
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
  }

  const enviar = (el: HTMLElement) =>
    el.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));

  it('deve cadastrar a peça com o custo por unidade', async () => {
    const { el, estabilizar } = await abrir();
    expect(el.querySelector('label[for="custo"]')?.textContent).toContain('Custo por unidade (R$)');

    digitar(el, 'nome', 'Fecho lagosta');
    digitar(el, 'custo', '42.9');
    enviar(el);
    await estabilizar();

    expect(servico.postPeca).toHaveBeenCalledWith(
      expect.objectContaining({ nome: 'Fecho lagosta', custo: 42.9 }),
    );
  });

  it('deve recusar custo negativo sem chamar o servidor', async () => {
    const { el, estabilizar } = await abrir();

    digitar(el, 'nome', 'Fecho lagosta');
    digitar(el, 'custo', '-1');
    enviar(el);
    await estabilizar();

    expect(servico.postPeca).not.toHaveBeenCalled();
    expect(el.textContent).toContain('O custo não pode ser negativo.');
  });

  it('deve abrir a peça salva e alterar o custo', async () => {
    const { el, estabilizar } = await abrir({ id: '7' });
    expect(servico.getPeca).toHaveBeenCalledWith(7, true);
    expect(el.querySelector<HTMLInputElement>('#nome')!.value).toBe('Turmalina 4 mm');
    expect(el.querySelector<HTMLInputElement>('#custo')!.value).toBe('85.5');
    expect(el.textContent).toContain('Salvar alterações');

    digitar(el, 'custo', '');
    enviar(el);
    await estabilizar();

    expect(servico.putPeca).toHaveBeenCalledWith(
      7,
      expect.objectContaining({
        nome: 'Turmalina 4 mm',
        tipo: 'PEDRA',
        material: 'Turmalina',
        custo: null,
      }),
    );
  });

  it('não deve mostrar nem enviar o custo sem precificacao.ler', async () => {
    const { el, estabilizar } = await abrir({ id: '7', veCusto: false });
    expect(servico.getPeca).toHaveBeenCalledWith(7, false);
    expect(el.querySelector('#custo')).toBeNull();

    enviar(el);
    await estabilizar();

    const [, dto] = servico.putPeca.mock.calls[0] as unknown as [number, Record<string, unknown>];
    expect(dto).not.toHaveProperty('custo');
  });
});

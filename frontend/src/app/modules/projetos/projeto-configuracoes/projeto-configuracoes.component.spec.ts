import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { BriefingService } from '@data/ferramentas/briefing/briefing.service';
import type { ProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { ProjetoConfiguracoesComponent } from './projeto-configuracoes.component';

describe('ProjetoConfiguracoesComponent', () => {
  const projeto: ProjetoDto = { id: 1, nome: 'Colar Raízes', descricao: null, cliente: { id: 3, nome: 'Maria Silva' }, versoes: [] };
  const servico = {
    getProjeto: vi.fn(() => of(projeto)),
    putCliente: vi.fn(() => of(undefined)),
  };

  async function abrir(podeEditar = true) {
    Object.values(servico).forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [ProjetoConfiguracoesComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        { provide: ProjetosService, useValue: servico },
        { provide: BriefingService, useValue: { getBriefing: () => of(null) } },
        { provide: ClientesService, useValue: { getClientesOpcoes: () => of([{ id: 3, nome: 'Maria Silva' }, { id: 4, nome: 'Ana Lima' }]) } },
        { provide: PermissaoService, useValue: { temAlguma: () => podeEditar } },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(ProjetoConfiguracoesComponent);
    fixture.componentRef.setInput('id', '1');
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable() };
  }

  const botao = (el: HTMLElement, texto: string) => [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim().startsWith(texto));

  it('deve mostrar o cliente definido e só abrir o seletor ao pedir para trocar', async () => {
    const { el, estabilizar } = await abrir();
    expect(el.querySelector('#cliente')).toBeNull();
    expect(el.querySelector('.definido')?.textContent).toContain('Maria Silva');

    el.querySelector<HTMLButtonElement>('[aria-label="Trocar cliente"]')!.click();
    await estabilizar();
    const seletor = el.querySelector<HTMLSelectElement>('#cliente')!;
    seletor.value = seletor.options[2]!.value;
    seletor.dispatchEvent(new Event('change'));
    await estabilizar();
    botao(el, 'Vincular')!.click();

    expect(servico.putCliente).toHaveBeenCalledWith(1, 4);
  });

  it('não deve oferecer troca de cliente para quem não tem a regra', async () => {
    const { el } = await abrir(false);

    expect(el.querySelector('[aria-label="Trocar cliente"]')).toBeNull();
    expect(el.textContent).toContain('Maria Silva');
  });
});

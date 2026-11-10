import { TestBed } from '@angular/core/testing';
import type { PrecosDoAtelieDto } from '@data/precificacao/precos/dto/precos.dto';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import type { PrecosDoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { PrecosProjetoComponent } from './precos-projeto.component';

describe('PrecosProjetoComponent', () => {
  const parametros = { perdaPercentual: 8, valorHora: 90, markup: 1.8, taxasPercentual: 0, arredondarPara: 10 };
  const herdados: PrecosDoProjetoDto = {
    herdadosEm: '2026-11-09T12:00:00',
    alteradosEm: null,
    cotacoes: [
      { metal: 'OURO', valor: 640, cotadaEm: '2026-11-08T12:00:00' },
      { metal: 'PRATA', valor: 6.8, cotadaEm: '2026-11-08T12:00:00' },
    ],
    parametros,
  };
  const atelieHoje: PrecosDoAtelieDto = {
    cotacoes: [
      { metal: 'OURO', atual: { valor: 655, registradaEm: '2026-11-10T12:00:00', registradaPor: 'Ana' } },
      { metal: 'PRATA', atual: { valor: 6.8, registradaEm: '2026-11-08T12:00:00', registradaPor: 'Ana' } },
    ],
    ligas: [],
    parametros,
  };
  const projetos = {
    getPrecosDoProjeto: vi.fn(() => of(herdados)),
    putPrecos: vi.fn(() => of(undefined)),
    postAtualizarPrecos: vi.fn(() => of(undefined)),
  };
  const precos = { getPrecos: vi.fn(() => of(atelieHoje)) };

  async function abrir(podeEditar = true) {
    [...Object.values(projetos), ...Object.values(precos)].forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [PrecosProjetoComponent],
      providers: [
        { provide: ProjetosService, useValue: projetos },
        { provide: PrecosService, useValue: precos },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(PrecosProjetoComponent);
    fixture.componentRef.setInput('projetoId', 1);
    fixture.componentRef.setInput('podeEditar', podeEditar);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable() };
  }

  // Normaliza os espaços (o pt-BR põe espaço não separável depois do "R$").
  const texto = (el: Element | null | undefined) => el?.textContent?.replace(/\s+/g, ' ').trim();
  const botao = (raiz: HTMLElement, rotulo: string) => [...raiz.querySelectorAll<HTMLButtonElement>('button')].find((b) => texto(b)?.startsWith(rotulo));
  const preencher = (raiz: HTMLElement, id: string, valor: string) => {
    const input = raiz.querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = valor;
    input.dispatchEvent(new Event('input'));
  };

  it('deve resumir os preços herdados com a data e mostrar o que o ateliê tem de diferente hoje', async () => {
    const { el } = await abrir();

    expect(texto(el.querySelector('.resumo'))).toBe('Ouro R$ 640,00/g · Prata R$ 6,80/g · Perda 8 % · Hora R$ 90,00 · Markup 1,8×');
    expect(texto(el.querySelector('.origem'))).toContain('herdado do ateliê em 09/11/2026');
    expect(texto(el.querySelector('.diferenca'))).toBe('Ateliê hoje: Ouro R$ 655,00/g');
  });

  it('deve editar só no projeto, enviando as cotações informadas e os parâmetros', async () => {
    const { el, estabilizar } = await abrir();

    botao(el, 'Editar')!.click();
    await estabilizar();
    preencher(el, 'cotacao-ouro', '700');
    preencher(el, 'cotacao-prata', '');
    preencher(el, 'precos-markup', '2.2');
    botao(el, 'Salvar preços')!.click();
    await estabilizar();

    expect(projetos.putPrecos).toHaveBeenCalledWith(1, {
      cotacoes: [{ metal: 'OURO', valor: 700 }],
      perdaPercentual: 8,
      valorHora: 90,
      markup: 2.2,
      taxasPercentual: 0,
      arredondarPara: 10,
    });
    expect(projetos.getPrecosDoProjeto).toHaveBeenCalledTimes(2);
    expect(el.querySelector('form')).toBeNull();
  });

  it('deve pedir confirmação antes de atualizar para os valores atuais do ateliê', async () => {
    const { el, estabilizar } = await abrir();

    botao(el, 'Atualizar para os valores atuais')!.click();
    await estabilizar();
    expect(projetos.postAtualizarPrecos).not.toHaveBeenCalled();
    expect(texto(el.querySelector('[role="alertdialog"]'))).toContain('Substituir as cotações e os parâmetros deste projeto');

    botao(el, 'Substituir')!.click();
    await estabilizar();

    expect(projetos.postAtualizarPrecos).toHaveBeenCalledWith(1);
    expect(el.querySelector('[role="alertdialog"]')).toBeNull();
  });

  it('deve mostrar quando o projeto foi editado e esconder as ações sem a regra de edição', async () => {
    projetos.getPrecosDoProjeto.mockReturnValueOnce(of({ ...herdados, alteradosEm: '2026-11-10T12:00:00' }));
    const { el } = await abrir(false);

    // Os trechos são spans de uma linha flex: o espaço em volta do "·" vem do gap, não do texto.
    expect(texto(el.querySelector('.origem'))).toMatch(/^herdado do ateliê em 09\/11\/2026\s*·\s*editado neste projeto em 10\/11\/2026$/);
    expect(botao(el, 'Editar')).toBeUndefined();
    expect(botao(el, 'Atualizar para os valores atuais')).toBeUndefined();
  });
});

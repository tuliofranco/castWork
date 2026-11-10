import { TestBed } from '@angular/core/testing';
import { PermissaoService } from '@core/services/permissao.service';
import type { PrecosDoAtelieDto } from '@data/precificacao/precos/dto/precos.dto';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { PrecificacaoComponent } from './precificacao.component';

const DIA_MS = 24 * 60 * 60 * 1000;

describe('PrecificacaoComponent', () => {
  const precos: PrecosDoAtelieDto = {
    cotacoes: [
      { metal: 'OURO', atual: { valor: 600, registradaEm: new Date(Date.now() - 20 * DIA_MS).toISOString(), registradaPor: 'Ana Lima' } },
      { metal: 'PRATA', atual: null },
    ],
    ligas: [
      { id: 2, nome: 'Ouro 18k amarelo', metal: 'OURO', teor: 750, densidade: 15.5 },
      { id: 7, nome: 'Prata 925', metal: 'PRATA', teor: 925, densidade: 10.36 },
    ],
    parametros: { perdaPercentual: 8, valorHora: 0, markup: 1.8, taxasPercentual: 0, arredondarPara: 10 },
  };
  const servico = {
    getPrecos: vi.fn(() => of(precos)),
    getHistoricoDeCotacoes: vi.fn(() => of({ totalCount: 0, pageInfo: { hasNextPage: false, endCursor: null }, nodes: [] })),
    postCotacao: vi.fn(() => of({ id: 1 })),
    postLiga: vi.fn(() => of({ id: 1000 })),
    putLiga: vi.fn(() => of(undefined)),
    desativarLiga: vi.fn(() => of(undefined)),
    putParametros: vi.fn(() => of(undefined)),
  };

  async function abrir(podeEditar = true) {
    Object.values(servico).forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [PrecificacaoComponent],
      providers: [
        { provide: PrecosService, useValue: servico },
        { provide: PermissaoService, useValue: { temAlguma: () => podeEditar } },
        { provide: ToastService, useValue: { sucesso: vi.fn(), erro: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(PrecificacaoComponent);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable() };
  }

  const texto = (el: Element | null | undefined) => el?.textContent?.replace(/\s+/g, ' ').trim();
  const botao = (raiz: HTMLElement, rotulo: string) =>
    [...raiz.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.getAttribute('aria-label') === rotulo || texto(b)?.startsWith(rotulo));
  const preencher = (input: HTMLInputElement | HTMLSelectElement, valor: string) => {
    input.value = valor;
    input.dispatchEvent(new Event(input instanceof HTMLSelectElement ? 'change' : 'input'));
  };

  it('deve mostrar as cotações com idade e aviso, as ligas e o aviso do valor da hora', async () => {
    const { el } = await abrir();
    const [ouro, prata] = [...el.querySelectorAll('cw-cotacao-metal')];

    expect(texto(ouro?.querySelector('.valor'))).toBe('R$ 600,00/g');
    expect(texto(ouro?.querySelector('.quando'))).toBe('Atualizada há 20 dias por Ana Lima');
    expect(texto(ouro?.querySelector('cw-selo'))).toBe('Mais de 15 dias sem atualizar');
    expect(texto(prata?.querySelector('.valor'))).toBe('Sem cotação');
    expect([...el.querySelectorAll('cw-ligas-atelie .meta')].map(texto)).toEqual([
      'Ouro · teor 750 (18k) · 15,5 g/cm³',
      'Prata · teor 925 (22.2k) · 10,36 g/cm³',
    ]);
    expect(texto(el.querySelector('cw-parametros-preco .aviso'))).toContain('Defina o valor da hora');
  });

  it('deve atualizar a cotação do metal e reler os preços', async () => {
    const { el, estabilizar } = await abrir();
    const ouro = el.querySelector<HTMLElement>('cw-cotacao-metal')!;

    botao(ouro, 'Atualizar')!.click();
    await estabilizar();
    preencher(ouro.querySelector<HTMLInputElement>('#valor-ouro')!, '612.35');
    botao(ouro, 'Salvar')!.click();
    await estabilizar();

    expect(servico.postCotacao).toHaveBeenCalledWith({ metal: 'OURO', valor: 612.35 });
    expect(servico.getPrecos).toHaveBeenCalledTimes(2);
  });

  it('deve editar uma liga pré-cadastrada', async () => {
    const { el, estabilizar } = await abrir();

    botao(el, 'Editar Ouro 18k amarelo')!.click();
    await estabilizar();
    expect(el.querySelector<HTMLInputElement>('#liga-teor')!.value).toBe('750');
    preencher(el.querySelector<HTMLInputElement>('#liga-nome')!, 'Ouro 18k amarelo (lei)');
    preencher(el.querySelector<HTMLInputElement>('#liga-densidade')!, '15.6');
    botao(el, 'Salvar liga')!.click();
    await estabilizar();

    expect(servico.putLiga).toHaveBeenCalledWith(2, { nome: 'Ouro 18k amarelo (lei)', metal: 'OURO', teor: '750', densidade: 15.6 });
  });

  it('deve pedir confirmação antes de desativar uma liga', async () => {
    const { el, estabilizar } = await abrir();

    botao(el, 'Desativar Prata 925')!.click();
    await estabilizar();
    const confirmacao = el.querySelector<HTMLElement>('[aria-label="Desativar Prata 925"][role="group"]')!;
    expect(servico.desativarLiga).not.toHaveBeenCalled();
    botao(confirmacao, 'Desativar')!.click();

    expect(servico.desativarLiga).toHaveBeenCalledWith(7);
  });

  it('deve salvar os parâmetros só com o valor da hora definido', async () => {
    const { el, estabilizar } = await abrir();

    botao(el, 'Salvar parâmetros')!.click();
    await estabilizar();
    expect(servico.putParametros).not.toHaveBeenCalled();
    preencher(el.querySelector<HTMLInputElement>('#valorHora')!, '85.5');
    botao(el, 'Salvar parâmetros')!.click();

    expect(servico.putParametros).toHaveBeenCalledWith({ perdaPercentual: 8, valorHora: 85.5, markup: 1.8, taxasPercentual: 0, arredondarPara: 10 });
  });

  it('deve ser só leitura para quem não tem a regra de edição', async () => {
    const { el } = await abrir(false);

    expect(texto(el.querySelector('.somente-leitura'))).toContain('Só consulta');
    expect(botao(el, 'Atualizar')).toBeUndefined();
    expect(botao(el, 'Editar Ouro 18k amarelo')).toBeUndefined();
    expect(botao(el, 'Nova liga')).toBeUndefined();
    expect(botao(el, 'Salvar parâmetros')).toBeUndefined();
    expect(el.querySelector<HTMLInputElement>('#markup')!.disabled).toBe(true);
    expect(botao(el, 'Histórico')).toBeDefined();
  });
});

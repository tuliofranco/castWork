import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { PrecosDoAtelieDto } from '@data/precificacao/precos/dto/precos.dto';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import type {
  CalculoDoPrecoDto,
  PrecoDaVersaoDto,
} from '@data/projetos/projetos/dto/preco-da-versao.dto';
import type { VersaoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { PrecoVersaoComponent } from './preco-versao.component';

describe('PrecoVersaoComponent', () => {
  const ouro = {
    ligaId: 2,
    liga: 'Ouro 18k amarelo',
    metal: 'OURO' as const,
    teor: 750,
    ligaAtiva: true,
    pesoEstimado: 5,
    pesoAFundir: 5.4,
    cotacaoDoPuro: 640,
    valorPorGrama: 480,
    custo: 2592,
    pesoReal: null,
    diferencaDoPesoReal: null,
  };
  /** O exemplo da issue #14. */
  const calculo: CalculoDoPrecoDto = {
    origemDoPeso: 'MANUAL',
    pesosManuaisGuardados: [],
    metais: [ouro],
    custoDosMetais: 2592,
    composicao: [
      { pecaDoProjetoId: 10, nome: 'Turmalina', quantidade: 1, custoUnitario: 500, custo: 500 },
      { pecaDoProjetoId: 11, nome: 'Fecho', quantidade: 2, custoUnitario: 150, custo: 300 },
    ],
    custoDaComposicao: 800,
    maoDeObra: { tipo: 'POR_HORAS', horas: 6, valorHora: 90, custo: 540 },
    servicos: [
      { nome: 'fundição', valor: 120 },
      { nome: 'ródio', valor: 60 },
    ],
    custoDosServicos: 180,
    custoTotal: 4112,
    perdaPercentual: 8,
    markup: 1.8,
    markupDaVersao: null,
    taxasPercentual: 6,
    arredondarPara: 10,
    precoSemArredondar: 7845.7,
    pendencias: [],
    avisos: [],
  };
  const completo: PrecoDaVersaoDto = { preco: 7850, status: 'COMPLETO', calculo };
  const versao: VersaoProjetoDto = {
    id: 20,
    numero: 1,
    nome: 'Base',
    observacoes: null,
    status: 'EM_DESENVOLVIMENTO',
    publicadaEm: null,
    itens: [],
  };
  const atelie: PrecosDoAtelieDto = {
    cotacoes: [],
    ligas: [
      { id: 2, nome: 'Ouro 18k amarelo', metal: 'OURO', teor: 750, densidade: 15.5 },
      { id: 3, nome: 'Ouro 18k branco', metal: 'OURO', teor: 750, densidade: 15 },
    ],
    parametros: {
      perdaPercentual: 8,
      valorHora: 90,
      markup: 1.8,
      taxasPercentual: 6,
      arredondarPara: 10,
    },
  };
  const projetos = {
    getPrecoDaVersao: vi.fn(() => of<PrecoDaVersaoDto | null>(completo)),
    putMetaisDaVersao: vi.fn(() => of(undefined)),
    putMaoDeObra: vi.fn(() => of(undefined)),
    putServicos: vi.fn(() => of(undefined)),
    putMarkupDaVersao: vi.fn(() => of(undefined)),
    putPesoReal: vi.fn(() => of(undefined)),
  };
  const precos = { getPrecos: vi.fn(() => of(atelie)) };

  async function abrir(
    opcoes: { podeVerCalculo?: boolean; podeEditar?: boolean; versao?: VersaoProjetoDto } = {},
  ) {
    [...Object.values(projetos), ...Object.values(precos)].forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [PrecoVersaoComponent],
      providers: [
        provideRouter([]),
        { provide: ProjetosService, useValue: projetos },
        { provide: PrecosService, useValue: precos },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(PrecoVersaoComponent);
    fixture.componentRef.setInput('projetoId', 1);
    fixture.componentRef.setInput('versao', opcoes.versao ?? versao);
    fixture.componentRef.setInput('podeVerCalculo', opcoes.podeVerCalculo ?? true);
    fixture.componentRef.setInput('podeEditar', opcoes.podeEditar ?? true);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable() };
  }

  // Normaliza os espaços (o pt-BR põe espaço não separável depois do "R$").
  const texto = (el: Element | null | undefined) => el?.textContent?.replace(/\s+/g, ' ').trim();
  const botao = (raiz: HTMLElement, rotulo: string) =>
    [...raiz.querySelectorAll<HTMLButtonElement>('button')].find((b) =>
      texto(b)?.startsWith(rotulo),
    );
  const linhas = (raiz: HTMLElement) =>
    [...raiz.querySelectorAll('.calculo tr')].map((tr) => [
      texto(tr.querySelector('th')),
      texto(tr.querySelector('td')),
    ]);
  const preencher = (raiz: HTMLElement, id: string, valor: string) => {
    const input = raiz.querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = valor;
    input.dispatchEvent(new Event('input'));
  };

  it('deve mostrar o cálculo linha a linha como no exemplo e o preço em destaque', async () => {
    const { el } = await abrir();

    expect(projetos.getPrecoDaVersao).toHaveBeenCalledWith(1, 20, true);
    expect(texto(el.querySelector('.destaque .valor'))).toBe('R$ 7.850,00');
    expect(texto(el.querySelector('cw-selo'))).toBe('Completo');
    expect(texto(el.querySelector('.dica'))).toContain('peso estimado');
    expect(
      linhas(el).map(([rotulo, valor]) => [rotulo?.replace(/ Registrar peso real$/, ''), valor]),
    ).toEqual([
      [
        'Ouro 18k amarelo: 5,00 g + perda 8% = 5,40 g × R$ 480,00/g (R$ 640,00 × 0,750) Peso estimado manualmente',
        'R$ 2.592,00',
      ],
      ['Composição: 3 unidades', 'R$ 800,00'],
      ['Mão de obra: 6 h × R$ 90,00', 'R$ 540,00'],
      ['Serviços: fundição R$ 120,00 + ródio R$ 60,00', 'R$ 180,00'],
      ['Custo total', 'R$ 4.112,00'],
      ['× 1,8 × 1,06, arredondado', 'R$ 7.850,00'],
    ]);
  });

  it('deve marcar Incompleto e listar as pendências com o atalho para resolver cada uma', async () => {
    projetos.getPrecoDaVersao.mockReturnValueOnce(
      of({
        preco: 5400,
        status: 'INCOMPLETO',
        calculo: {
          ...calculo,
          pendencias: [
            {
              tipo: 'PECA_SEM_VALOR',
              descricao: 'Turmalina está sem valor no projeto.',
              pecaDoProjetoId: 10,
              ligaId: null,
              metal: null,
              corpos: [],
            },
            {
              tipo: 'METAL_SEM_COTACAO',
              descricao: 'O projeto não tem cotação de prata.',
              pecaDoProjetoId: null,
              ligaId: null,
              metal: 'PRATA',
              corpos: [],
            },
            {
              tipo: 'LIGA_DESATIVADA',
              descricao: 'A liga Ouro 22k foi desativada: troque por outra.',
              pecaDoProjetoId: null,
              ligaId: 1000,
              metal: null,
              corpos: [],
            },
          ],
          avisos: [
            {
              tipo: 'COTACAO_ANTIGA',
              descricao: 'A cotação de ouro tem 20 dias: confira antes de orçar.',
              corpos: [],
            },
          ],
        },
      }),
    );
    const { el, estabilizar } = await abrir();

    expect(texto(el.querySelector('cw-selo'))).toBe('Incompleto');
    const pendencias = [...el.querySelectorAll('.pendencias li')];
    expect(pendencias.map((li) => texto(li.querySelector('span')))).toEqual([
      'Turmalina está sem valor no projeto.',
      'O projeto não tem cotação de prata.',
      'A liga Ouro 22k foi desativada: troque por outra.',
    ]);
    expect(pendencias[0].querySelector('a')!.getAttribute('href')).toBe('/projetos/1/pecas/10');
    expect(pendencias[1].querySelector('a')!.getAttribute('href')).toBe('/projetos/1');
    expect(texto(el.querySelector('.aviso-preco'))).toContain('20 dias');

    botao(pendencias[2] as HTMLElement, 'Trocar a liga')!.click();
    await estabilizar();
    expect(el.querySelector('form[aria-label="Metais e peso estimado"]')).not.toBeNull();
  });

  it('deve mostrar o peso da peça pelo 3D e avisar que o peso manual guardado não é usado', async () => {
    projetos.getPrecoDaVersao.mockReturnValueOnce(
      of({
        ...completo,
        calculo: {
          ...calculo,
          origemDoPeso: 'MODELO',
          pesosManuaisGuardados: [{ ligaId: 2, liga: 'Ouro 18k amarelo', peso: 5 }],
          metais: [
            { ...ouro, pesoEstimado: 2.824, pesoAFundir: 3.05, custo: 1464 },
            {
              ...ouro,
              ligaId: 3,
              liga: 'Ouro 18k branco',
              pesoEstimado: 0.5,
              pesoAFundir: 0.54,
              custo: 259.2,
            },
          ],
          avisos: [
            {
              tipo: 'CORPOS_SOBREPOSTOS',
              descricao:
                'Aro e Garra se sobrepõem (19,4 mm³): a parte comum conta duas vezes no peso.',
              corpos: ['Aro', 'Garra'],
            },
          ],
        },
      }),
    );
    const { el, estabilizar } = await abrir();

    expect(texto(el.querySelector('.dica'))).toContain('medido no modelo 3D');
    const metais = [...el.querySelectorAll('.calculo tr.metal')];
    expect(texto(metais[0].querySelector('.origem-do-peso'))).toBe(
      'Peso da peça (3D) · medido no modelo',
    );
    expect(texto(metais[0])).toContain('Ouro 18k amarelo: 2,824 g + perda 8% = 3,05 g');
    expect(texto(el.querySelector('.manual-guardado'))).toBe(
      'Peso manual guardado e não usado (vale o 3D): Ouro 18k amarelo 5,00 g.',
    );
    expect(texto(el.querySelector('.aviso-preco'))).toContain('se sobrepõem');
    expect(el.querySelector('.aviso-preco a')!.getAttribute('href')).toBe(
      '/projetos/1/versoes/20/modelo-3d?conferir=1',
    );
    expect(el.querySelector('.aviso-preco i')!.className).toContain('bi-intersect');
    // Peso real: só das ligas com peso manual guardado (a do 3D sem peso manual não tem onde registrar).
    expect(botao(metais[0] as HTMLElement, 'Registrar peso real')).toBeDefined();
    expect(botao(metais[1] as HTMLElement, 'Registrar peso real')).toBeUndefined();

    botao(el, 'Metais e peso')!.click();
    await estabilizar();
    expect(el.querySelector<HTMLInputElement>('#metal-peso-0')!.value).toBe('5');
    expect(el.querySelector('#metal-peso-1')).toBeNull();
    expect(texto(el.querySelector('form[aria-label="Metais e peso estimado"] .dica'))).toContain(
      'vale o peso medido nele',
    );
  });

  it('deve deixar o preço incompleto com corpos sem material e atalho para a conferência', async () => {
    projetos.getPrecoDaVersao.mockReturnValueOnce(
      of({
        preco: 3000,
        status: 'INCOMPLETO',
        calculo: {
          ...calculo,
          origemDoPeso: 'MODELO',
          pendencias: [
            {
              tipo: 'CORPOS_SEM_MATERIAL',
              descricao: '2 corpos sem material no modelo 3D: Garra, Pino.',
              pecaDoProjetoId: null,
              ligaId: null,
              metal: null,
              corpos: ['Garra', 'Pino'],
            },
            {
              tipo: 'SEM_METAL_COM_PESO',
              descricao: 'O modelo 3D não tem nenhum corpo de metal com peso.',
              pecaDoProjetoId: null,
              ligaId: null,
              metal: null,
              corpos: [],
            },
          ],
          metais: [],
        },
      }),
    );
    const { el } = await abrir();

    expect(texto(el.querySelector('cw-selo'))).toBe('Incompleto');
    const pendencias = [...el.querySelectorAll<HTMLElement>('.pendencias li')];
    expect(texto(pendencias[0].querySelector('span'))).toBe(
      '2 corpos sem material no modelo 3D: Garra, Pino.',
    );
    expect(texto(pendencias[0].querySelector('a'))).toBe('Conferir');
    expect(pendencias[0].querySelector('a')!.getAttribute('href')).toBe(
      '/projetos/1/versoes/20/modelo-3d?conferir=1',
    );
    expect(texto(pendencias[1].querySelector('a'))).toBe('Abrir o 3D');
    expect(botao(pendencias[1], 'Informar os metais')).toBeUndefined();
    expect(texto(el.querySelector('.calculo tr th'))).toBe('Metal: nenhum medido no 3D');
  });

  it('deve mostrar só o preço e o status sem a regra de consultar precificação', async () => {
    projetos.getPrecoDaVersao.mockReturnValueOnce(of({ preco: 7850, status: 'COMPLETO' }));
    const { el } = await abrir({ podeVerCalculo: false, podeEditar: false });

    expect(projetos.getPrecoDaVersao).toHaveBeenCalledWith(1, 20, false);
    expect(texto(el.querySelector('.destaque .valor'))).toBe('R$ 7.850,00');
    expect(texto(el.querySelector('cw-selo'))).toBe('Completo');
    expect(el.querySelector('.calculo')).toBeNull();
    expect(botao(el, 'Metais e peso')).toBeUndefined();
  });

  it('deve mostrar o peso real ao lado do estimado com a diferença', async () => {
    projetos.getPrecoDaVersao.mockReturnValueOnce(
      of({
        ...completo,
        calculo: { ...calculo, metais: [{ ...ouro, pesoReal: 5.2, diferencaDoPesoReal: 4 }] },
      }),
    );
    const { el, estabilizar } = await abrir({ versao: { ...versao, status: 'PUBLICADA' } });

    expect(texto(el.querySelector('.peso-real'))).toBe('estimado 5,00 g · real 5,20 g · +4%');
    expect(botao(el, 'Metais e peso')).toBeUndefined();

    botao(el, 'Alterar peso real')!.click();
    await estabilizar();
    preencher(el, 'peso-real-2', '5.25');
    botao(el, 'Salvar peso real')!.click();
    await estabilizar();

    expect(projetos.putPesoReal).toHaveBeenCalledWith(1, 20, 2, { pesoGramas: 5.25 });
  });

  it('deve alternar a mão de obra entre horas e valor fixo', async () => {
    const { el, estabilizar } = await abrir();

    botao(el, 'Mão de obra')!.click();
    await estabilizar();
    expect(el.querySelector<HTMLInputElement>('#mao-de-obra-horas')!.value).toBe('6');
    el.querySelector<HTMLInputElement>('input[value="VALOR_FIXO"]')!.click();
    await estabilizar();
    expect(el.querySelector('#mao-de-obra-horas')).toBeNull();
    preencher(el, 'mao-de-obra-valor', '700');
    botao(el, 'Salvar mão de obra')!.click();
    await estabilizar();

    expect(projetos.putMaoDeObra).toHaveBeenCalledWith(1, 20, {
      tipo: 'VALOR_FIXO',
      horas: null,
      valor: 700,
    });
    expect(projetos.getPrecoDaVersao).toHaveBeenCalledTimes(2);
  });

  it('deve editar os metais com as ligas ativas, os serviços e o markup da versão', async () => {
    const { el, estabilizar } = await abrir();

    botao(el, 'Metais e peso')!.click();
    await estabilizar();
    expect([...el.querySelectorAll('#metal-liga-0 option')].map((o) => texto(o))).toEqual([
      'Escolha a liga',
      'Ouro 18k amarelo',
      'Ouro 18k branco',
    ]);
    botao(el, 'Adicionar liga')!.click();
    await estabilizar();
    const select = el.querySelector<HTMLSelectElement>('#metal-liga-1')!;
    select.value = select.options[2].value;
    select.dispatchEvent(new Event('change'));
    preencher(el, 'metal-peso-1', '1.5');
    botao(el, 'Salvar metais')!.click();
    await estabilizar();
    expect(projetos.putMetaisDaVersao).toHaveBeenCalledWith(1, 20, {
      metais: [
        { ligaId: 2, pesoGramas: 5 },
        { ligaId: 3, pesoGramas: 1.5 },
      ],
    });

    botao(el, 'Serviços')!.click();
    await estabilizar();
    preencher(el, 'servico-valor-1', '80');
    botao(el, 'Salvar serviços')!.click();
    await estabilizar();
    expect(projetos.putServicos).toHaveBeenCalledWith(1, 20, {
      servicos: [
        { nome: 'fundição', valor: 120 },
        { nome: 'ródio', valor: 80 },
      ],
    });

    botao(el, 'Markup')!.click();
    await estabilizar();
    preencher(el, 'markup-versao', '2.2');
    botao(el, 'Salvar markup')!.click();
    await estabilizar();
    expect(projetos.putMarkupDaVersao).toHaveBeenCalledWith(1, 20, { markup: 2.2 });
  });
});

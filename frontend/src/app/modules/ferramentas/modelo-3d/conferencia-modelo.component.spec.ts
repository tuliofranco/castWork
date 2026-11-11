import { TestBed } from '@angular/core/testing';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import type { ConferenciaDoModeloDto } from '@data/ferramentas/modelo-3d/dto/conferencia-do-modelo.dto';
import { Modelo3dService } from '@data/ferramentas/modelo-3d/modelo-3d.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { ConferenciaModeloComponent } from './conferencia-modelo.component';

describe('ConferenciaModeloComponent', () => {
  const materiais = [
    { nome: 'Castwork liga 2', rotulo: 'Ouro 18k amarelo', uso: 'METAL' as const, ligaId: 2 },
    { nome: 'Castwork liga 3', rotulo: 'Ouro 18k branco', uso: 'METAL' as const, ligaId: 3 },
    { nome: 'Pedra', rotulo: 'Pedra', uso: 'PEDRA' as const, ligaId: null },
    { nome: 'Apoio (não conta)', rotulo: 'Apoio (não conta)', uso: 'APOIO' as const, ligaId: null },
  ];
  /** O exemplo da issue #17. */
  const anel: ConferenciaDoModeloDto = {
    arquivoId: 9,
    medido: true,
    alteravel: true,
    pesoDoMetal: 5,
    pesosPorLiga: [{ ligaId: 2, liga: 'Ouro 18k amarelo', peso: 5 }],
    corpos: [
      { nome: 'Aro', material: 'Castwork liga 2', uso: 'METAL', ligaId: 2, liga: 'Ouro 18k amarelo', volumeMm3: 322.581, massaGramas: 5, fechado: true, contaNoPeso: true },
      { nome: 'Chaton', material: 'Default', uso: 'SEM_MATERIAL', ligaId: null, liga: null, volumeMm3: 41.2, massaGramas: 0.053, fechado: true, contaNoPeso: false },
      { nome: 'Pedra central', material: 'Pedra', uso: 'PEDRA', ligaId: null, liga: null, volumeMm3: 28, massaGramas: 0.098, fechado: true, contaNoPeso: false },
      { nome: 'Guia', material: 'Apoio (não conta)', uso: 'APOIO', ligaId: null, liga: null, volumeMm3: null, massaGramas: null, fechado: false, contaNoPeso: false },
    ],
    sobreposicoes: [{ corpoA: 'Aro', corpoB: 'Chaton', volumeMm3: 0.4, deMetal: false, podeUnir: false }],
    materiais,
  };
  /** Depois de aplicar o ouro no chaton: os dois são da mesma liga e se sobrepõem. */
  const comChaton: ConferenciaDoModeloDto = {
    ...anel,
    arquivoId: 10,
    pesoDoMetal: 5.639,
    corpos: anel.corpos.map((c) =>
      c.nome === 'Chaton' ? { ...c, material: 'Castwork liga 2', uso: 'METAL' as const, ligaId: 2, liga: 'Ouro 18k amarelo', massaGramas: 0.639, contaNoPeso: true } : c,
    ),
    sobreposicoes: [{ corpoA: 'Chaton', corpoB: 'Aro', volumeMm3: 0.4, deMetal: true, podeUnir: true }],
  };
  const modelo3d = {
    getConferencia: vi.fn(() => of<ConferenciaDoModeloDto | null>(anel)),
    putMateriais: vi.fn(() => of({ arquivoId: 10, arquivoAnteriorId: 9 })),
    postUnirCorpos: vi.fn(() => of({ arquivoId: 11, arquivoAnteriorId: 10 })),
  };
  const ferramentas = { postRestaurar: vi.fn(() => of<number | null>(12)) };

  beforeAll(() => {
    // O jsdom não abre <dialog> modal.
    HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
      this.open = true;
    };
    HTMLDialogElement.prototype.close ??= function (this: HTMLDialogElement) {
      this.open = false;
    };
  });

  async function abrir(opcoes: { podeEditar?: boolean } = {}) {
    [...Object.values(modelo3d), ...Object.values(ferramentas)].forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [ConferenciaModeloComponent],
      providers: [
        { provide: Modelo3dService, useValue: modelo3d },
        { provide: FerramentasService, useValue: ferramentas },
        { provide: ToastService, useValue: { sucesso: vi.fn(), erro: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(ConferenciaModeloComponent);
    const alterados: number[] = [];
    fixture.componentInstance.alterado.subscribe((id) => alterados.push(id));
    fixture.componentRef.setInput('projetoId', 1);
    fixture.componentRef.setInput('versaoId', 20);
    fixture.componentRef.setInput('podeEditar', opcoes.podeEditar ?? true);
    fixture.componentRef.setInput('aberta', true);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable(), alterados };
  }

  const texto = (el: Element | null | undefined) => el?.textContent?.replace(/\s+/g, ' ').trim();
  const botao = (raiz: HTMLElement, rotulo: string) => [...raiz.querySelectorAll<HTMLButtonElement>('button')].find((b) => texto(b) === rotulo);
  const linhas = (raiz: HTMLElement) =>
    [...raiz.querySelectorAll('.corpos tbody tr')].map((tr) => [...tr.querySelectorAll('th, td')].map((c) => texto(c.querySelector('select') ? null : c) ?? ''));
  const escolher = (raiz: HTMLElement, corpo: string, material: string) => {
    const select = raiz.querySelector<HTMLSelectElement>(`select[aria-label="Material de ${corpo}"]`)!;
    select.value = material;
    select.dispatchEvent(new Event('change'));
  };

  it('deve listar os corpos com material, volume e peso e destacar os sem material', async () => {
    modelo3d.getConferencia.mockReturnValueOnce(of(anel));
    const { el } = await abrir({ podeEditar: false });

    expect(modelo3d.getConferencia).toHaveBeenCalledWith(1, 20);
    expect(texto(el.querySelector('.peso-do-metal'))).toBe('Peso do metal: 5,00 g');
    expect(linhas(el)).toEqual([
      ['Aro', 'Ouro 18k amarelo', '322,6 mm³', '5,00 g'],
      ['Chaton', '— sem material —', '41,2 mm³', 'sem material'],
      ['Pedra central', 'Pedra', '28 mm³', 'fora do peso'],
      ['Guia', 'Apoio (não conta)', '', 'aberto: fora do peso'],
    ]);
    expect(el.querySelectorAll('tr.sem-material')).toHaveLength(1);
    expect(texto(el.querySelector('.sobreposicao'))).toContain('Aro e Chaton se sobrepõem (0,4 mm³)');
    expect(el.querySelector('select')).toBeNull();
    expect(botao(el, 'Aplicar')).toBeUndefined();
  });

  it('deve aplicar o material escolhido em um ou vários corpos e oferecer o desfazer', async () => {
    modelo3d.getConferencia.mockReturnValueOnce(of(anel)).mockReturnValueOnce(of(comChaton));
    const { el, estabilizar, alterados } = await abrir();

    expect(texto(el.querySelector('.dica.pendente'))).toContain('1 corpo sem material');
    expect(botao(el, 'Aplicar')!.disabled).toBe(true);
    escolher(el, 'Chaton', 'Castwork liga 2');
    escolher(el, 'Guia', 'Pedra');
    await estabilizar();
    expect(texto(el.querySelector('footer small'))).toBe('2 corpos para aplicar');

    botao(el, 'Aplicar')!.click();
    await estabilizar();

    expect(modelo3d.putMateriais).toHaveBeenCalledWith(1, 20, {
      arquivoId: 9,
      corpos: [
        { corpo: 'Chaton', material: 'Castwork liga 2' },
        { corpo: 'Guia', material: 'Pedra' },
      ],
    });
    expect(alterados).toEqual([10]);
    expect(modelo3d.getConferencia).toHaveBeenCalledTimes(2);
    expect(el.querySelectorAll('tr.sem-material')).toHaveLength(0);
    expect(el.querySelector('.dica.pendente')).toBeNull();
    expect(texto(el.querySelector('.peso-do-metal'))).toBe('Peso do metal: 5,639 g');
    expect(texto(el.querySelector('.feito'))).toContain('Material aplicado em 2 corpos.');

    botao(el, 'Desfazer')!.click();
    await estabilizar();
    expect(ferramentas.postRestaurar).toHaveBeenCalledWith(1, 20, 'modelo-3d', 9);
    expect(alterados).toEqual([10, 12]);
  });

  it('deve unir corpos de metal sobrepostos só depois de confirmar', async () => {
    modelo3d.getConferencia.mockReturnValueOnce(of(comChaton)).mockReturnValueOnce(of({ ...comChaton, arquivoId: 11, sobreposicoes: [] }));
    const { el, estabilizar, alterados } = await abrir();

    botao(el, 'Unir corpos')!.click();
    await estabilizar();
    expect(modelo3d.postUnirCorpos).not.toHaveBeenCalled();
    expect(texto(el.querySelector('[role="alertdialog"]'))).toContain('Unir Aro e Chaton? Viram um corpo só, "Aro"');
    expect(texto(el.querySelector('[role="alertdialog"]'))).toContain('desfazer pelo histórico');

    botao(el, 'Unir')!.click();
    await estabilizar();

    // O maior primeiro: o corpo unido fica com o nome dele.
    expect(modelo3d.postUnirCorpos).toHaveBeenCalledWith(1, 20, { arquivoId: 10, corpos: ['Aro', 'Chaton'] });
    expect(alterados).toEqual([11]);
    expect(el.querySelector('.sobreposicao')).toBeNull();
    expect(texto(el.querySelector('.feito'))).toContain('Aro e Chaton viraram um corpo só.');
  });

  it('não deve oferecer unir corpos sem material ou de ligas diferentes', async () => {
    modelo3d.getConferencia.mockReturnValueOnce(of(anel));
    const { el } = await abrir();

    expect(botao(el, 'Unir corpos')).toBeUndefined();
    expect(texto(el.querySelector('.sobreposicao small'))).toBe('Defina o material para unir.');
  });

  it('deve ficar só leitura na versão publicada ou arquivada', async () => {
    modelo3d.getConferencia.mockReturnValueOnce(of({ ...comChaton, alteravel: false }));
    const { el } = await abrir();

    expect(texto(el.querySelector('.dica'))).toContain('só para conferir');
    expect(el.querySelector('select')).toBeNull();
    expect(botao(el, 'Unir corpos')).toBeUndefined();
    expect(botao(el, 'Aplicar')).toBeUndefined();
  });
});

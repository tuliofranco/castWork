import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import type { PecaDoProjetoDto } from '@data/projetos/projetos/dto/peca-do-projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { PecasProjetoComponent } from './pecas-projeto.component';

registerLocaleData(localePt, 'pt-BR');

describe('PecasProjetoComponent', () => {
  const pecas: PecaDoProjetoDto[] = [
    { id: 1, nome: 'Elo oval', tipo: 'ELO', ligaId: 2, material: 'Ouro 18k amarelo', teor: 750, pesoGramas: 0.35, descricao: null, ativo: true, status: 'PUBLICADA', pecaId: 40, valor: 12.5 },
    { id: 2, nome: 'Elo torcido', tipo: 'ELO', ligaId: null, material: null, teor: null, pesoGramas: null, descricao: null, ativo: true, status: 'RASCUNHO', pecaId: null, valor: null },
  ];
  const biblioteca = {
    totalCount: 2,
    pageInfo: { hasNextPage: false, endCursor: null },
    nodes: [
      { id: 40, nome: 'Elo oval', tipo: 'ELO', material: 'Ouro 18k amarelo', teor: 750, pesoGramas: 0.35, ativo: true },
      { id: 41, nome: 'Turmalina', tipo: 'PEDRA', material: null, teor: null, pesoGramas: null, ativo: true },
    ],
  };
  const servico = {
    getPecasDoProjeto: vi.fn(() => of(pecas)),
    postImportarPeca: vi.fn(() => of({ id: 3 })),
    putValorDaPeca: vi.fn(() => of(undefined)),
    postPublicarPeca: vi.fn(() => of({ id: 42 })),
  };

  async function abrir(entradas: Partial<Record<'podeEditar' | 'podeImportar' | 'podeVerValor' | 'podeEditarValor', boolean>> = {}) {
    Object.values(servico).forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [PecasProjetoComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: ProjetosService, useValue: servico },
        { provide: PecasService, useValue: { getPecas: vi.fn(() => of(biblioteca)) } },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(PecasProjetoComponent);
    fixture.componentRef.setInput('projetoId', 1);
    const todas = { podeEditar: true, podeImportar: true, podeVerValor: true, podeEditarValor: true, ...entradas };
    Object.entries(todas).forEach(([nome, valor]) => fixture.componentRef.setInput(nome, valor));
    const alterado = vi.fn();
    fixture.componentInstance.alterado.subscribe(alterado);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const linhas = () =>
      [...el.querySelectorAll('.peca')].map((linha) =>
        [...linha.querySelectorAll('.nome, .meta, cw-selo, .valor')].map((parte) => parte.textContent?.replace(/\s+/g, ' ').trim()),
      );
    return { el, linhas, alterado, estabilizar: () => fixture.whenStable() };
  }

  const botao = (raiz: Element, texto: string) => [...raiz.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim().startsWith(texto));

  it('deve listar as peças com o selo Rascunho/Publicada e o valor no projeto', async () => {
    const { el, linhas } = await abrir();

    expect(linhas()).toEqual([
      ['Elo oval', 'Elo · Ouro 18k amarelo · 750', 'Publicada', 'R$ 12,50'],
      ['Elo torcido', 'Elo', 'Rascunho', 'Sem valor'],
    ]);
    expect(servico.getPecasDoProjeto).toHaveBeenCalledWith(1, true);
    expect([...el.querySelectorAll('.peca a.abrir')].map((a) => a.getAttribute('href'))).toEqual(['/pecas/1', '/pecas/2']);
    expect(el.querySelector('a.btn-primary')!.getAttribute('href')).toBe('/pecas/nova');
  });

  it('deve publicar só a peça em rascunho', async () => {
    const { el, alterado, estabilizar } = await abrir();
    const [publicada, rascunho] = [...el.querySelectorAll('.peca')];

    expect(botao(publicada!, 'Publicar')).toBeUndefined();
    botao(rascunho!, 'Publicar')!.click();
    await estabilizar();

    expect(servico.postPublicarPeca).toHaveBeenCalledWith(1, 2);
    expect(alterado).toHaveBeenCalled();
  });

  it('deve definir o valor da peça na própria lista', async () => {
    const { el, estabilizar } = await abrir();
    const linha = el.querySelectorAll('.peca')[1]!;

    botao(linha, 'Definir valor')!.click();
    await estabilizar();
    const campo = linha.querySelector<HTMLInputElement>('#valor-2')!;
    campo.value = '40';
    campo.dispatchEvent(new Event('input'));
    linha.querySelector('form')!.dispatchEvent(new Event('submit'));
    await estabilizar();

    expect(servico.putValorDaPeca).toHaveBeenCalledWith(1, 2, { valor: 40 });
  });

  it('deve importar da biblioteca marcando a peça que já está no projeto', async () => {
    const { el, alterado, estabilizar } = await abrir();

    botao(el, 'Importar da biblioteca')!.click();
    await estabilizar();
    const resultados = [...el.querySelectorAll('.importar .resultado')];
    expect(resultados.map((r) => r.querySelector('cw-selo')?.textContent?.trim() ?? botao(r, 'Importar')?.textContent?.trim())).toEqual(['No projeto', 'Importar']);

    botao(resultados[1]!, 'Importar')!.click();
    await estabilizar();

    expect(servico.postImportarPeca).toHaveBeenCalledWith(1, 41);
    expect(alterado).toHaveBeenCalled();
  });

  it('não deve pedir nem mostrar o valor sem precificacao.ler, nem oferecer ações sem as regras', async () => {
    const { el, linhas } = await abrir({ podeEditar: false, podeImportar: false, podeVerValor: false, podeEditarValor: false });

    expect(servico.getPecasDoProjeto).toHaveBeenCalledWith(1, false);
    expect(linhas()).toEqual([
      ['Elo oval', 'Elo · Ouro 18k amarelo · 750', 'Publicada'],
      ['Elo torcido', 'Elo', 'Rascunho'],
    ]);
    expect(el.querySelectorAll('button')).toHaveLength(0);
    expect(el.textContent).not.toContain('Nova peça');
  });
});

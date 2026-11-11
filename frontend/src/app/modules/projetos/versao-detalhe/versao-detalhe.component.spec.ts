import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { BriefingService } from '@data/ferramentas/briefing/briefing.service';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import type { ProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { VersaoDetalheComponent } from './versao-detalhe.component';

// Mesmo locale do app (app.config): números com vírgula decimal.
registerLocaleData(localePt, 'pt-BR');

describe('VersaoDetalheComponent', () => {
  const elo = { id: 10, nome: 'Elo oval', tipo: 'ELO' as const, material: 'Ouro amarelo', teor: 750, pesoGramas: 0.5, ativo: true, status: 'PUBLICADA' as const };
  const torcido = { id: 11, nome: 'Elo torcido', tipo: 'ELO' as const, material: null, teor: null, pesoGramas: null, ativo: true, status: 'RASCUNHO' as const };
  const desativada = { id: 12, nome: 'Fecho antigo', tipo: 'FECHO' as const, material: null, teor: null, pesoGramas: null, ativo: false, status: 'PUBLICADA' as const };
  const projeto: ProjetoDto = {
    id: 1,
    nome: 'Colar Raízes',
    descricao: null,
    cliente: null,
    pecas: [elo, torcido, desativada],
    versoes: [
      { id: 22, numero: 3, nome: 'Rascunho', observacoes: null, status: 'ARQUIVADA', publicadaEm: null, itens: [] },
      { id: 21, numero: 2, nome: 'Com pedra', observacoes: null, status: 'EM_DESENVOLVIMENTO', publicadaEm: null, itens: [] },
      {
        id: 23,
        numero: 4,
        nome: 'Torcida',
        observacoes: null,
        status: 'EM_DESENVOLVIMENTO',
        publicadaEm: null,
        itens: [
          { id: 102, quantidade: 8, observacao: null, peca: torcido },
          { id: 103, quantidade: 1, observacao: null, peca: elo },
        ],
      },
      {
        id: 20,
        numero: 1,
        nome: 'Versão 1',
        observacoes: null,
        status: 'PUBLICADA',
        publicadaEm: '2026-11-05T13:00:00Z',
        itens: [{ id: 100, quantidade: 12, observacao: 'ródio', peca: elo }],
      },
    ],
  };
  const servico = {
    getProjeto: vi.fn(() => of(projeto)),
    postItem: vi.fn(() => of({ id: 101 })),
    postPublicarVersao: vi.fn(() => of(undefined)),
    postArquivarVersao: vi.fn(() => of(undefined)),
    postDuplicarVersao: vi.fn(() => of({ id: 30 })),
    getPrecoDaVersao: vi.fn(() => of({ preco: 0, status: 'INCOMPLETO' as const, calculo: null })),
    // O elo tem modelo 3D: vale o peso medido nele (0,6 g), não o digitado (0,5 g).
    getPesosDaVersao: vi.fn(() => of(new Map([[10, 0.6]]))),
  };

  async function abrir(versaoId: string, podeEditar = true) {
    Object.values(servico).forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [VersaoDetalheComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: ProjetosService, useValue: servico },
        { provide: PermissaoService, useValue: { temAlguma: () => podeEditar } },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
        { provide: BriefingService, useValue: { getBriefing: () => of(null) } },
        { provide: PrecosService, useValue: { getPrecos: () => of(null) } },
      ],
    });
    const fixture = TestBed.createComponent(VersaoDetalheComponent);
    fixture.componentRef.setInput('id', '1');
    fixture.componentRef.setInput('versaoId', versaoId);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable() };
  }

  const botao = (el: HTMLElement, texto: string) => [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim() === texto);

  it('deve mostrar a versão em desenvolvimento com as peças do projeto e voltar para o projeto', async () => {
    const { el } = await abrir('21');

    expect(el.querySelector('h1')!.textContent).toBe('2. Com pedra');
    expect(el.querySelector('.voltar')!.textContent).toContain('Colar Raízes');
    expect(el.querySelector('.voltar')!.getAttribute('href')).toBe('/projetos/1');
    expect(el.querySelector('.area')!.classList).toContain('com-biblioteca');
    expect(botao(el, 'Publicar versão')!.disabled).toBe(false);
  });

  it('deve somar o peso da composição com o peso que vale para cada peça', async () => {
    const { el } = await abrir('20');

    expect(servico.getPesosDaVersao).toHaveBeenCalledWith(1, 20);
    expect(el.querySelector('.cabecalho-secao .resumo')!.textContent).toContain('7,200 g');
  });

  it('deve adicionar a peça com a quantidade escolhida no stepper', async () => {
    const { el, estabilizar } = await abrir('21');
    const mais = el.querySelector<HTMLButtonElement>('.biblioteca button[aria-label="Aumentar Elo oval"]')!;

    mais.click();
    mais.click();
    await estabilizar();
    botao(el, 'Adicionar')!.click();

    expect(servico.postItem).toHaveBeenCalledWith(1, 21, { pecaDoProjetoId: 10, quantidade: 3 });
  });

  it('deve oferecer só as peças ativas do projeto para a composição', async () => {
    const { el } = await abrir('21');

    expect([...el.querySelectorAll('.biblioteca .resultado .nome')].map((n) => n.textContent?.trim())).toEqual(['Elo oval', 'Elo torcido']);
    expect(el.querySelector('.biblioteca h2')!.textContent).toBe('Adicionar do projeto');
    expect(el.querySelector('.biblioteca a')!.getAttribute('href')).toBe('/projetos/1/pecas/nova');
  });

  it('deve recusar publicar com peça em rascunho mostrando a peça com atalho', async () => {
    const { el, estabilizar } = await abrir('23');
    expect(el.querySelector('.composicao .cw-linha cw-selo')!.textContent).toContain('Rascunho');

    botao(el, 'Publicar versão')!.click();
    await estabilizar();

    expect(servico.postPublicarVersao).not.toHaveBeenCalled();
    const aviso = el.querySelector('cw-pecas-em-rascunho')!;
    expect(aviso.textContent).toContain('Para publicar a versão 4');
    expect([...aviso.querySelectorAll('a')].map((a) => [a.textContent?.trim(), a.getAttribute('href')])).toEqual([['Elo torcido', '/projetos/1/pecas/11']]);
  });

  it('deve publicar a versão sem peça em rascunho', async () => {
    const { el, estabilizar } = await abrir('21');

    botao(el, 'Publicar versão')!.click();
    await estabilizar();

    expect(servico.postPublicarVersao).toHaveBeenCalledWith(1, 21);
    expect(el.querySelector('cw-pecas-em-rascunho')).toBeNull();
  });

  it('deve arquivar e voltar para o projeto', async () => {
    const { el, estabilizar } = await abrir('21');

    botao(el, 'Arquivar')!.click();
    await estabilizar();

    expect(servico.postArquivarVersao).toHaveBeenCalledWith(1, 21);
    expect(TestBed.inject(Router).url).toBe('/projetos/1');
  });

  it('deve mostrar a versão publicada congelada, sem arquivar nem biblioteca', async () => {
    const { el } = await abrir('20');

    expect(el.textContent).toContain('Composição congelada.');
    expect(el.querySelector('.biblioteca')).toBeNull();
    expect(botao(el, 'Arquivar')).toBeUndefined();
    const linha = el.querySelector('.composicao .cw-linha')!;
    expect([...linha.querySelectorAll('.nome, .meta, .observacao, .quantidade-fixa')].map((p) => p.textContent?.replace(/\s+/g, ' ').trim())).toEqual([
      'Elo oval',
      'Elo · Ouro amarelo · 750',
      'ródio',
      '12×',
    ]);
    expect(el.querySelector('.composicao .resumo')!.textContent?.replace(/\s+/g, ' ').trim()).toBe('1 peça · 12 un. · 7,200 g');
  });

  it('deve duplicar e abrir a cópia', async () => {
    const { el, estabilizar } = await abrir('20');

    botao(el, 'Duplicar')!.click();
    await estabilizar();

    expect(servico.postDuplicarVersao).toHaveBeenCalledWith(1, 20);
    expect(TestBed.inject(Router).url).toBe('/projetos/1/versoes/30');
  });

  it('deve mostrar a versão arquivada sem edição e com desarquivar', async () => {
    const { el } = await abrir('22');

    expect(el.textContent).toContain('Versão arquivada');
    expect(botao(el, 'Desarquivar')).toBeDefined();
    expect(botao(el, 'Duplicar')).toBeUndefined();
    expect(el.querySelector('.biblioteca')).toBeNull();
  });

  it('deve avisar quando a versão não existe no projeto', async () => {
    const { el } = await abrir('999');

    expect(el.querySelector('h1')!.textContent).toBe('Versão não encontrada');
  });
});

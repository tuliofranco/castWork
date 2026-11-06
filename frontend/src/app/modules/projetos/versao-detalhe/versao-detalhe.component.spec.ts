import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { BriefingService } from '@data/ferramentas/briefing/briefing.service';
import type { ProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { VersaoDetalheComponent } from './versao-detalhe.component';

// Mesmo locale do app (app.config): números com vírgula decimal.
registerLocaleData(localePt, 'pt-BR');

describe('VersaoDetalheComponent', () => {
  const elo = { id: 10, nome: 'Elo oval', tipo: 'ELO' as const, material: 'Ouro amarelo', teor: 750, pesoGramas: 0.5 };
  const projeto: ProjetoDto = {
    id: 1,
    nome: 'Colar Raízes',
    descricao: null,
    cliente: null,
    versoes: [
      { id: 22, numero: 3, nome: 'Rascunho', observacoes: null, status: 'ARQUIVADA', publicadaEm: null, itens: [] },
      { id: 21, numero: 2, nome: 'Com pedra', observacoes: null, status: 'EM_DESENVOLVIMENTO', publicadaEm: null, itens: [] },
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
    postArquivarVersao: vi.fn(() => of(undefined)),
    postDuplicarVersao: vi.fn(() => of({ id: 30 })),
  };

  async function abrir(versaoId: string, podeEditar = true) {
    Object.values(servico).forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [VersaoDetalheComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: ProjetosService, useValue: servico },
        {
          provide: PecasService,
          useValue: { getPecas: () => of({ totalCount: 1, pageInfo: { hasNextPage: false, endCursor: null }, nodes: [{ ...elo, ativo: true }] }) },
        },
        { provide: PermissaoService, useValue: { temAlguma: () => podeEditar } },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
        { provide: BriefingService, useValue: { getBriefing: () => of(null) } },
      ],
    });
    const fixture = TestBed.createComponent(VersaoDetalheComponent);
    fixture.componentRef.setInput('id', '1');
    fixture.componentRef.setInput('versaoId', versaoId);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable() };
  }

  const botao = (el: HTMLElement, texto: string) => [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim() === texto);

  it('deve mostrar a versão em desenvolvimento com a biblioteca e voltar para o projeto', async () => {
    const { el } = await abrir('21');

    expect(el.querySelector('h1')!.textContent).toBe('v2 · Com pedra');
    expect(el.querySelector('.voltar')!.textContent).toContain('Colar Raízes');
    expect(el.querySelector('.voltar')!.getAttribute('href')).toBe('/projetos/1');
    expect(el.querySelector('.area')!.classList).toContain('com-biblioteca');
    expect(botao(el, 'Publicar versão')!.disabled).toBe(true);
  });

  it('deve adicionar a peça com a quantidade escolhida no stepper', async () => {
    const { el, estabilizar } = await abrir('21');
    const mais = el.querySelector<HTMLButtonElement>('.biblioteca button[aria-label="Aumentar Elo oval"]')!;

    mais.click();
    mais.click();
    await estabilizar();
    botao(el, 'Adicionar')!.click();

    expect(servico.postItem).toHaveBeenCalledWith(1, 21, { pecaId: 10, quantidade: 3 });
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
      'Elo · Ouro amarelo · 750 (18k)',
      'ródio',
      '12×',
    ]);
    expect(el.querySelector('.composicao .resumo')!.textContent?.replace(/\s+/g, ' ').trim()).toBe('1 peça · 12 un. · 6,000 g');
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

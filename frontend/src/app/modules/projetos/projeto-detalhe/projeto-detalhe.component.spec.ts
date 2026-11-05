import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import type { ProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { ProjetoDetalheComponent } from './projeto-detalhe.component';

// Mesmo locale do app (app.config): números com vírgula decimal.
registerLocaleData(localePt, 'pt-BR');

describe('ProjetoDetalheComponent', () => {
  const elo = { id: 10, nome: 'Elo oval', tipo: 'ELO' as const, material: 'Ouro amarelo', teor: 750, pesoGramas: 0.5 };
  const projeto: ProjetoDto = {
    id: 1,
    nome: 'Colar Raízes',
    descricao: null,
    cliente: { id: 3, nome: 'Maria Silva' },
    versoes: [
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
  const postItem = vi.fn();

  async function abrir(podeEditar = true): Promise<{ el: HTMLElement; detectar: () => Promise<void> }> {
    postItem.mockReset().mockReturnValue(of({ id: 101 }));
    TestBed.configureTestingModule({
      imports: [ProjetoDetalheComponent],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: ProjetosService, useValue: { getProjeto: () => of(projeto), postItem } },
        {
          provide: PecasService,
          useValue: { getPecas: () => of({ totalCount: 1, pageInfo: { hasNextPage: false, endCursor: null }, nodes: [{ ...elo, ativo: true }] }) },
        },
        { provide: PermissaoService, useValue: { temAlguma: () => podeEditar } },
        { provide: ToastService, useValue: { sucesso: vi.fn(), aviso: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(ProjetoDetalheComponent);
    fixture.componentRef.setInput('id', '1');
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    return { el, detectar: () => fixture.whenStable() };
  }

  it('deve abrir na versão mais recente, em desenvolvimento, com a busca de peças', async () => {
    const { el } = await abrir();

    expect([...el.querySelectorAll('[role=tab]')].map((t) => t.textContent?.trim())).toEqual(['v2', 'v1']);
    expect(el.textContent).toContain('Com pedra');
    expect(el.textContent).toContain('Nenhuma peça nesta versão ainda.');
    expect(el.textContent).toContain('Adicionar peça da biblioteca');
    expect(el.querySelector<HTMLButtonElement>('.btn-success')!.disabled).toBe(true);
  });

  it('deve adicionar a peça encontrada com a quantidade informada', async () => {
    const { el } = await abrir();
    const quantidade = el.querySelector<HTMLInputElement>('input[aria-label="Quantidade de Elo oval"]')!;
    quantidade.value = '12';

    [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Adicionar')!.click();

    expect(postItem).toHaveBeenCalledWith(1, 21, { pecaId: 10, quantidade: 12 });
  });

  it('deve mostrar a versão publicada congelada, sem edição nem busca', async () => {
    const { el, detectar } = await abrir();

    (el.querySelectorAll<HTMLButtonElement>('[role=tab]')[1]).click();
    await detectar();

    expect(el.textContent).toContain('a composição está congelada');
    expect(el.textContent).not.toContain('Adicionar peça da biblioteca');
    expect([...el.querySelectorAll('tbody tr td')].map((td) => td.textContent?.replace(/\s+/g, ' ').trim())).toEqual([
      'Elo oval ródio',
      'Elo',
      'Ouro amarelo · 750 (18k)',
      '12',
    ]);
    expect(el.textContent).toContain('6,000 g');
  });

  it('não deve oferecer edição para quem não tem a regra', async () => {
    const { el } = await abrir(false);

    expect(el.textContent).not.toContain('Adicionar peça da biblioteca');
    expect(el.textContent).not.toContain('Duplicar como nova versão');
  });
});

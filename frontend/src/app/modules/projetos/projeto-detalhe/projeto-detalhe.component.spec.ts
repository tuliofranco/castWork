import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import type { ProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { BriefingService } from '@data/projetos/briefing/briefing.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { ProjetoDetalheComponent } from './projeto-detalhe.component';

describe('ProjetoDetalheComponent', () => {
  const peca = { id: 10, nome: 'Elo', tipo: 'ELO' as const, material: null, teor: null, pesoGramas: null };
  const projeto: ProjetoDto = {
    id: 1,
    nome: 'Colar Raízes',
    descricao: 'Inspirado em raízes',
    cliente: { id: 3, nome: 'Maria Silva' },
    versoes: [
      { id: 23, numero: 3, nome: 'Rascunho', observacoes: null, status: 'ARQUIVADA', publicadaEm: null, itens: [] },
      { id: 22, numero: 2, nome: 'Com pedra', observacoes: null, status: 'EM_DESENVOLVIMENTO', publicadaEm: null, itens: [{ id: 1, quantidade: 2, observacao: null, peca }] },
      { id: 21, numero: 1, nome: 'Versão 1', observacoes: null, status: 'PUBLICADA', publicadaEm: '2026-11-05T13:00:00Z', itens: [{ id: 2, quantidade: 1, observacao: null, peca }] },
    ],
  };
  const servico = {
    getProjeto: vi.fn(() => of(projeto)),
    postArquivarVersao: vi.fn(() => of(undefined)),
    postDesarquivarVersao: vi.fn(() => of(undefined)),
    postPublicarVersao: vi.fn(() => of(undefined)),
    postVersao: vi.fn(() => of({ id: 24 })),
  };

  async function abrir(podeEditar = true) {
    Object.values(servico).forEach((f) => f.mockClear());
    TestBed.configureTestingModule({
      imports: [ProjetoDetalheComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        { provide: ProjetosService, useValue: servico },
        { provide: BriefingService, useValue: { getBriefing: () => of(null) } },
        { provide: PermissaoService, useValue: { temAlguma: () => podeEditar } },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(ProjetoDetalheComponent);
    fixture.componentRef.setInput('id', '1');
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable() };
  }

  const versoes = (el: HTMLElement) =>
    [...el.querySelectorAll('.versao')].map((v) => [...v.querySelectorAll('.numero, .nome, cw-selo')].map((p) => p.textContent?.trim()).join(' | '));
  const botao = (el: HTMLElement, texto: string) => [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim().startsWith(texto));

  it('deve listar só as versões não arquivadas e mostrá-las sob demanda', async () => {
    const { el, estabilizar } = await abrir();

    expect(versoes(el)).toEqual(['v2 | Com pedra | Em desenvolvimento', 'v1 | Versão 1 | Publicada']);

    botao(el, 'Mostrar arquivadas (1)')!.click();
    await estabilizar();

    expect(versoes(el)[0]).toBe('v3 | Rascunho | Arquivada');
  });

  it('deve oferecer publicar e arquivar só na versão em desenvolvimento, e desarquivar na arquivada', async () => {
    const { el, estabilizar } = await abrir();
    botao(el, 'Mostrar arquivadas')!.click();
    await estabilizar();
    const [arquivada, emDesenvolvimento, publicada] = [...el.querySelectorAll('.versao .acoes')].map((a) => a.textContent?.replace(/\s+/g, ' ').trim());

    el.querySelector<HTMLButtonElement>('[aria-label="Arquivar versão 2"]')!.click();
    botao(el, 'Desarquivar')!.click();

    expect(arquivada).toBe('Desarquivar');
    expect(emDesenvolvimento).toBe('Publicar');
    expect(publicada).toBe('');
    expect(servico.postArquivarVersao).toHaveBeenCalledWith(1, 22);
    expect(servico.postDesarquivarVersao).toHaveBeenCalledWith(1, 23);
  });

  it('deve criar uma versão e abrir a página dela', async () => {
    const { el, estabilizar } = await abrir();

    botao(el, 'Nova versão')!.click();
    await estabilizar();

    expect(servico.postVersao).toHaveBeenCalledWith(1, { nome: 'Versão 4' });
    expect(TestBed.inject(Router).url).toBe('/projetos/1/versoes/24');
  });

  it('não deve oferecer edição para quem não tem a regra', async () => {
    const { el } = await abrir(false);

    expect(el.querySelector('#cliente')).toBeNull();
    expect(el.textContent).toContain('Maria Silva');
    expect(el.querySelectorAll('.versao .acoes')).toHaveLength(0);
    expect(botao(el, 'Nova versão')).toBeUndefined();
  });
});

import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { Component, LOCALE_ID, input, output } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import type { PecaDoProjetoDto } from '@data/projetos/projetos/dto/peca-do-projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { PainelPdfComponent } from '@shared/ui/painel-pdf.component';
import { of } from 'rxjs';

import { PecaDoProjetoComponent } from './peca-do-projeto.component';

registerLocaleData(localePt, 'pt-BR');

/** O painel de PDF sem o diálogo modal nem o visualizador: mostra só se recebeu um arquivo e as ações projetadas. */
@Component({
  selector: 'cw-painel-pdf',
  template: `@if (arquivo()) {
    <div class="painel-aberto" [attr.data-titulo]="titulo()"><ng-content /></div>
  }`,
})
class PainelPdfDeTeste {
  readonly arquivo = input<Blob | null>(null);
  readonly titulo = input('');
  readonly fechado = output();
}

describe('PecaDoProjetoComponent', () => {
  const turmalina: PecaDoProjetoDto = {
    id: 7,
    nome: 'Turmalina 4 mm',
    tipo: 'PEDRA',
    ligaId: null,
    material: 'Turmalina',
    teor: null,
    pesoGramas: null,
    descricao: null,
    ativo: true,
    status: 'PUBLICADA',
    pecaId: 40,
    valor: 85.5,
  };
  const rascunho: PecaDoProjetoDto = {
    ...turmalina,
    id: 8,
    nome: 'Elo torcido',
    tipo: 'ELO',
    ligaId: 2,
    material: 'Ouro 18k amarelo',
    teor: 750,
    status: 'RASCUNHO',
    pecaId: null,
    valor: null,
  };
  /** Publicada com uma liga que o ateliê desativou depois (não vem entre as ativas). */
  const comLigaDesativada: PecaDoProjetoDto = { ...rascunho, id: 9, nome: 'Elo antigo', ligaId: 99, material: 'Ouro 22k', teor: 916, status: 'PUBLICADA', pecaId: 41 };
  const ligas = [
    { id: 2, nome: 'Ouro 18k amarelo', metal: 'OURO' as const, teor: 750, densidade: 15.5 },
    { id: 3, nome: 'Ouro 18k branco', metal: 'OURO' as const, teor: 750, densidade: 15 },
  ];

  const servico = {
    getPecasDoProjeto: vi.fn(() => of([turmalina, rascunho, comLigaDesativada])),
    postPecaDoProjeto: vi.fn(() => of({ id: 9 })),
    putPecaDoProjeto: vi.fn(() => of(undefined)),
    putValorDaPeca: vi.fn(() => of(undefined)),
    postPublicarPeca: vi.fn(() => of({ id: 41 })),
    getPesoDaPeca: vi.fn(() => of({ gramas: 1.86, origem: 'MODELO' as const, pesoDigitado: 1 })),
    getArquivosFinaisDaPeca: vi.fn(() => of([{ ferramenta: 'modelo-3d', arquivoId: 30, nome: 'Elo fino', criadoEm: '2026-11-10T12:00:00Z' }])),
    getFichaTecnicaDaPeca: vi.fn(() => of(new Blob(['%PDF-1.7'], { type: 'application/pdf' }))),
  };

  async function abrir(opcoes: { readonly pecaId?: string; readonly permissoes?: readonly string[] } = {}) {
    Object.values(servico).forEach((f) => f.mockClear());
    const permissoes = opcoes.permissoes ?? ['projetos.editar', 'precificacao.ler', 'precificacao.editar'];
    TestBed.configureTestingModule({
      imports: [PecaDoProjetoComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: ProjetosService, useValue: servico },
        { provide: PrecosService, useValue: { getLigas: () => of(ligas) } },
        { provide: PermissaoService, useValue: { temAlguma: (pedidas: readonly string[]) => pedidas.some((p) => permissoes.includes(p)) } },
        { provide: ToastService, useValue: { sucesso: vi.fn() } },
      ],
    });
    TestBed.overrideComponent(PecaDoProjetoComponent, { remove: { imports: [PainelPdfComponent] }, add: { imports: [PainelPdfDeTeste] } });
    const fixture = TestBed.createComponent(PecaDoProjetoComponent);
    fixture.componentRef.setInput('id', '1');
    if (opcoes.pecaId) fixture.componentRef.setInput('pecaId', opcoes.pecaId);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, estabilizar: () => fixture.whenStable() };
  }

  function digitar(el: HTMLElement, id: string, valor: string): void {
    const campo = el.querySelector<HTMLInputElement>(`#${id}`)!;
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
  }

  /** Escolhe a opção do seletor de liga pelo texto. */
  function escolherLiga(el: HTMLElement, texto: string): void {
    const select = el.querySelector<HTMLSelectElement>('#ligaId')!;
    select.selectedIndex = [...select.options].findIndex((o) => o.textContent?.trim() === texto);
    select.dispatchEvent(new Event('change'));
  }

  const opcoesDeLiga = (el: HTMLElement) => [...el.querySelectorAll<HTMLOptionElement>('#ligaId option')].map((o) => o.textContent?.trim());
  const ligaEscolhida = (el: HTMLElement) => el.querySelector<HTMLSelectElement>('#ligaId')!.selectedOptions[0]?.textContent?.trim();

  const enviar = (el: HTMLElement, seletor = 'form.cw-formulario') => el.querySelector<HTMLFormElement>(seletor)!.dispatchEvent(new Event('submit'));
  const botao = (el: HTMLElement, texto: string) => [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim().startsWith(texto));

  it('deve criar a peça no projeto com a liga escolhida, em rascunho e sem valor, e abrir a página dela', async () => {
    const { el, estabilizar } = await abrir();
    expect(el.querySelector('h1')?.textContent).toContain('Nova peça');
    expect(el.querySelector('#valor')).toBeNull();
    expect(el.querySelector('#teor')).toBeNull();
    expect(opcoesDeLiga(el)).toEqual(['Sem liga', 'Ouro 18k amarelo', 'Ouro 18k branco']);

    digitar(el, 'nome', 'Fecho lagosta');
    escolherLiga(el, 'Ouro 18k branco');
    await estabilizar();
    expect(el.textContent).toContain('Teor 750 (18k), da liga.');
    enviar(el);
    await estabilizar();

    expect(servico.postPecaDoProjeto).toHaveBeenCalledWith(1, { nome: 'Fecho lagosta', tipo: 'ELO', ligaId: 3, material: null, pesoGramas: null, descricao: null });
    expect(servico.postPecaDoProjeto.mock.calls[0]).not.toContainEqual(expect.objectContaining({ valor: expect.anything() }));
    expect(TestBed.inject(Router).url).toBe('/projetos/1/pecas/9');
  });

  it('deve ter material livre e nenhuma liga na pedra', async () => {
    const { el, estabilizar } = await abrir();

    [...el.querySelectorAll('label.opcao')].find((l) => l.textContent?.trim() === 'Pedra')!.querySelector('input')!.click();
    await estabilizar();
    expect(el.querySelector('#ligaId')).toBeNull();
    digitar(el, 'nome', 'Turmalina 4 mm');
    digitar(el, 'material', 'Turmalina');
    enviar(el);
    await estabilizar();

    expect(servico.postPecaDoProjeto).toHaveBeenCalledWith(1, expect.objectContaining({ tipo: 'PEDRA', ligaId: null, material: 'Turmalina' }));
  });

  it('deve manter a liga gravada que foi desativada como a escolhida e salvar sem aviso', async () => {
    const { el, estabilizar } = await abrir({ pecaId: '9' });
    expect(ligaEscolhida(el)).toBe('Ouro 22k (desativada)');
    expect(el.textContent).toContain('Teor 916, da liga.');

    digitar(el, 'pesoGramas', '');
    el.querySelector<HTMLInputElement>('input[type="checkbox"]')!.click();
    enviar(el);
    await estabilizar();

    expect(el.querySelector('[role="alertdialog"]')).toBeNull();
    expect(servico.putPecaDoProjeto).toHaveBeenCalledWith(1, 9, expect.objectContaining({ ligaId: 99, ativo: false }));
  });

  it('deve avisar que trocar a liga de uma peça publicada faz dela outra peça', async () => {
    const { el, estabilizar } = await abrir({ pecaId: '9' });

    escolherLiga(el, 'Ouro 18k amarelo');
    enviar(el);
    await estabilizar();

    expect(servico.putPecaDoProjeto).not.toHaveBeenCalled();
    expect(el.querySelector('[role="alertdialog"]')?.textContent).toContain('faz dela outra peça');
  });

  it('deve avisar antes de salvar que a peça publicada vira outra peça', async () => {
    const { el, estabilizar } = await abrir({ pecaId: '7' });
    expect(el.querySelector('cw-selo')?.textContent).toContain('Publicada');
    expect(el.querySelector<HTMLInputElement>('#nome')!.value).toBe('Turmalina 4 mm');

    digitar(el, 'nome', 'Turmalina 5 mm');
    enviar(el);
    await estabilizar();

    expect(servico.putPecaDoProjeto).not.toHaveBeenCalled();
    expect(el.querySelector('[role="alertdialog"]')?.textContent).toContain('faz dela outra peça');

    botao(el, 'Salvar como outra peça')!.click();
    await estabilizar();

    expect(servico.putPecaDoProjeto).toHaveBeenCalledWith(1, 7, expect.objectContaining({ nome: 'Turmalina 5 mm', ativo: true }));
  });

  it('deve salvar sem aviso quando só o ativo muda na peça publicada', async () => {
    const { el, estabilizar } = await abrir({ pecaId: '7' });

    const ativa = el.querySelector<HTMLInputElement>('input[type="checkbox"]')!;
    ativa.click();
    enviar(el);
    await estabilizar();

    expect(el.querySelector('[role="alertdialog"]')).toBeNull();
    expect(servico.putPecaDoProjeto).toHaveBeenCalledWith(1, 7, expect.objectContaining({ nome: 'Turmalina 4 mm', ativo: false }));
  });

  it('deve definir o valor no projeto e recusar valor negativo', async () => {
    const { el, estabilizar } = await abrir({ pecaId: '7' });
    expect(el.querySelector<HTMLInputElement>('#valor')!.value).toBe('85.5');

    digitar(el, 'valor', '-1');
    enviar(el, 'form.linha-valor');
    await estabilizar();
    expect(servico.putValorDaPeca).not.toHaveBeenCalled();
    expect(el.textContent).toContain('O valor não pode ser negativo.');

    digitar(el, 'valor', '90');
    enviar(el, 'form.linha-valor');
    await estabilizar();
    expect(servico.putValorDaPeca).toHaveBeenCalledWith(1, 7, { valor: 90 });
  });

  it('deve publicar a peça em rascunho', async () => {
    const { el, estabilizar } = await abrir({ pecaId: '8' });
    expect(botao(el, 'Publicar')).toBeDefined();

    botao(el, 'Publicar')!.click();
    await estabilizar();

    expect(servico.postPublicarPeca).toHaveBeenCalledWith(1, 8);
  });

  it('não deve mostrar o valor sem precificacao.ler nem deixar editar sem projetos.editar', async () => {
    const { el } = await abrir({ pecaId: '8', permissoes: ['projetos.ler'] });

    expect(servico.getPecasDoProjeto).toHaveBeenCalledWith(1, false);
    expect(el.textContent).not.toContain('Valor no projeto');
    expect(el.querySelector<HTMLInputElement>('#nome')!.disabled).toBe(true);
    expect(botao(el, 'Publicar')).toBeUndefined();
    expect(botao(el, 'Salvar')).toBeUndefined();
  });

  it('deve mostrar as ferramentas da peça e de onde vem o peso', async () => {
    const { el } = await abrir({ pecaId: '9' });

    const ferramentas = [...el.querySelectorAll<HTMLAnchorElement>('a.ferramenta')];
    expect(ferramentas.map((a) => a.getAttribute('href'))).toEqual(['/desenho-2d', '/modelo-3d'].map((f) => expect.stringContaining(f)));
    expect(el.textContent).toContain('Ferramentas da peça');
    expect(servico.getPesoDaPeca).toHaveBeenCalledWith(1, 9);
    expect(el.querySelector('.peso')?.textContent?.replace(/\s+/g, ' ').trim()).toBe(
      'Peso: 1,860 g · medido no modelo 3D, na liga da peça (o peso digitado fica guardado e não é usado)',
    );
  });

  it('deve mostrar em cada cartão qual arquivo é o final', async () => {
    const { el } = await abrir({ pecaId: '9' });

    const [desenho, modelo] = [...el.querySelectorAll<HTMLAnchorElement>('a.ferramenta')];
    expect(servico.getArquivosFinaisDaPeca).toHaveBeenCalledWith(1, 9);
    expect(desenho.querySelector('.sem-final')?.textContent?.trim()).toBe('Sem arquivo final');
    expect(modelo.querySelector('.final')?.textContent?.trim()).toBe('Final: Elo fino');
  });

  it('não deve mostrar as ferramentas no cadastro da peça nova', async () => {
    const { el } = await abrir();

    expect(el.querySelector('a.ferramenta')).toBeNull();
    expect(servico.getPesoDaPeca).not.toHaveBeenCalled();
  });

  describe('Documentos', () => {
    function botao(el: HTMLElement, rotulo: string): HTMLButtonElement {
      return el.querySelector<HTMLButtonElement>(`button[aria-label="${rotulo}"]`)!;
    }

    it('mostra a ficha técnica depois das ferramentas da peça', async () => {
      const { el } = await abrir({ pecaId: '8', permissoes: [] });

      const titulos = [...el.querySelectorAll('section.bloco h2')].map((h) => h.textContent?.trim());
      expect(titulos.indexOf('Documentos')).toBe(titulos.indexOf('Ferramentas da peça') + 1);
      expect(el.querySelector('.documento')?.textContent).toContain('Ficha técnica');
      expect(botao(el, 'Ver ficha técnica')).not.toBeNull();
      expect(botao(el, 'Baixar ficha técnica')).not.toBeNull();
      expect(servico.getFichaTecnicaDaPeca).not.toHaveBeenCalled();
    });

    it('Ver gera a ficha e abre no painel de PDF', async () => {
      const { el, estabilizar } = await abrir({ pecaId: '8' });

      botao(el, 'Ver ficha técnica').click();
      await estabilizar();

      expect(servico.getFichaTecnicaDaPeca).toHaveBeenCalledWith(1, 8);
      expect(el.querySelector('.painel-aberto')?.getAttribute('data-titulo')).toBe('Ficha técnica · Elo torcido');
    });

    it('Baixar gera a ficha e salva com o nome da peça', async () => {
      const criarUrl = vi.fn(() => 'blob:ficha');
      const revogar = vi.fn();
      Object.assign(URL, { createObjectURL: criarUrl, revokeObjectURL: revogar });
      const clicar = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
        expect(this.download).toBe('ficha-tecnica-elo-torcido.pdf');
      });
      const { el, estabilizar } = await abrir({ pecaId: '8' });

      botao(el, 'Baixar ficha técnica').click();
      await estabilizar();

      expect(servico.getFichaTecnicaDaPeca).toHaveBeenCalledWith(1, 8);
      expect(clicar).toHaveBeenCalledOnce();
      expect(revogar).toHaveBeenCalledWith('blob:ficha');
      expect(el.querySelector('.painel-aberto')).toBeNull();
      clicar.mockRestore();
    });

    it('não aparece no cadastro de peça nova', async () => {
      const { el } = await abrir();

      expect(el.querySelector('#titulo-documentos')).toBeNull();
    });
  });
});

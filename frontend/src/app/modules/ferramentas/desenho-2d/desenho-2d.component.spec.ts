import { ChangeDetectionStrategy, Component, forwardRef, input, output, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { PermissaoService } from '@core/services/permissao.service';
import type { VersaoDoArquivoDto } from '@data/assistente/dto/ferramenta.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { DownloadService } from '@shared/services/download.service';
import { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { ConversaIaComponent } from '../../assistente/conversa-ia.component';

import { Desenho2dComponent } from './desenho-2d.component';
import { HistoricoDesenhoComponent } from './historico-desenho.component';
import { PonteCadCraft } from './ponte-cadcraft';

/** A conversa de verdade fala com a API; aqui basta o que o Desenho 2D usa dela. */
@Component({
  selector: 'cw-conversa-ia',
  template: '<ng-content select="[acoes]" /><ng-content select="[area]" />',
  providers: [{ provide: ConversaIaComponent, useExisting: forwardRef(() => ConversaFalsaComponent) }],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class ConversaFalsaComponent {
  readonly ferramenta = input<string>();
  readonly titulo = input<string>();
  readonly projetoId = input<number>();
  readonly escopo = input<unknown>();
  readonly voltarPara = input<string>();
  readonly voltarRotulo = input<string>();
  readonly antesDeEnviar = input<unknown>();
  readonly arquivoMudou = output();
  readonly pensando = signal(false);
  readonly registrarArquivo = vi.fn();
}

describe('Desenho2dComponent', () => {
  const dxf = new Blob(['0\nEOF\n']);
  const ferramentas = {
    getArquivo: vi.fn(() => of({ id: 50, nome: 'Desenho', conteudo: dxf })),
    putArquivo: vi.fn(() => of(51)),
    postRestaurar: vi.fn(() => of(52)),
  };
  const toast = { sucesso: vi.fn(), erro: vi.fn() };

  /** Abre o desenho da versão com o CADCraft simulado; `comAlteracoes` é o "•" no título dele. */
  async function abrir(comAlteracoes: boolean) {
    vi.restoreAllMocks();
    [...Object.values(ferramentas), ...Object.values(toast)].forEach((f) => f.mockClear());
    const carregar = vi.spyOn(PonteCadCraft.prototype, 'carregar').mockResolvedValue();
    vi.spyOn(PonteCadCraft.prototype, 'descarregar').mockImplementation(() => undefined);
    vi.spyOn(PonteCadCraft.prototype, 'temAlteracoes').mockReturnValue(comAlteracoes);
    vi.spyOn(PonteCadCraft.prototype, 'salvar').mockResolvedValue(dxf);

    TestBed.configureTestingModule({
      imports: [Desenho2dComponent],
      providers: [
        { provide: FerramentasService, useValue: ferramentas },
        { provide: ToastService, useValue: toast },
        { provide: DownloadService, useValue: {} },
        { provide: ProjetosService, useValue: {} },
        { provide: PermissaoService, useValue: { temAlguma: () => true } },
      ],
    });
    TestBed.overrideComponent(Desenho2dComponent, { remove: { imports: [ConversaIaComponent] }, add: { imports: [ConversaFalsaComponent] } });
    const fixture = TestBed.createComponent(Desenho2dComponent);
    fixture.componentRef.setInput('id', '1');
    fixture.componentRef.setInput('versaoId', '20');
    await fixture.whenStable();

    fixture.debugElement.query(By.css('iframe')).triggerEventHandler('load');
    await vi.waitFor(() => expect(carregar).toHaveBeenCalledTimes(1));
    await fixture.whenStable();
    return { fixture, el: fixture.nativeElement as HTMLElement, carregar };
  }

  const versao = { id: 7 } as VersaoDoArquivoDto;

  async function restaurar(fixture: Awaited<ReturnType<typeof abrir>>['fixture']) {
    fixture.debugElement.query(By.directive(HistoricoDesenhoComponent)).triggerEventHandler('restaurar', versao);
    await vi.waitFor(() => expect(toast.sucesso).toHaveBeenCalledWith('Desenho restaurado.'));
    await fixture.whenStable();
  }

  const botao = (el: HTMLElement, texto: string) => [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim() === texto);

  it('deve liberar a prancheta depois de restaurar com alterações feitas à mão', async () => {
    const { fixture, el, carregar } = await abrir(true);

    await restaurar(fixture);

    // O que foi desenhado à mão é guardado antes, e o desenho restaurado abre no outro CADCraft.
    expect(ferramentas.putArquivo).toHaveBeenCalledTimes(1);
    expect(ferramentas.postRestaurar).toHaveBeenCalledWith(1, 20, 'desenho-2d', 7);
    expect(carregar).toHaveBeenCalledTimes(2);
    expect(el.querySelector('.sincronizando')).toBeNull();
    expect(botao(el, 'Salvar')!.disabled).toBe(false);
    expect(botao(el, 'Histórico')!.disabled).toBe(false);
  });

  it('deve restaurar sem guardar nada quando não há alterações feitas à mão', async () => {
    const { fixture, el } = await abrir(false);

    await restaurar(fixture);

    expect(ferramentas.putArquivo).not.toHaveBeenCalled();
    expect(el.querySelector('.sincronizando')).toBeNull();
    expect(botao(el, 'Salvar')!.disabled).toBe(false);
  });
});

import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { PermissaoService } from '@core/services/permissao.service';
import type { VersaoDoArquivoDto } from '@data/assistente/dto/ferramenta.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { Modelo3dService } from '@data/ferramentas/modelo-3d/modelo-3d.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { DownloadService } from '@shared/services/download.service';
import { ToastService } from '@shared/services/toast.service';
import { of, throwError } from 'rxjs';

import { ConversaIaComponent } from '../../assistente/conversa-ia.component';
import { ConversaFalsaComponent } from '../../assistente/testing/conversa-falsa.component';

import { HistoricoModeloComponent } from './historico-modelo.component';
import { Modelo3dComponent } from './modelo-3d.component';
import { PonteSolidCraft } from './ponte-solidcraft';

describe('Modelo3dComponent', () => {
  const projeto3d = new Blob(['{}'], { type: 'application/json' });
  const ferramentas = {
    getArquivo: vi.fn(() => of({ id: 50, nome: 'Modelo', conteudo: projeto3d })),
    putArquivo: vi.fn(() => of(51)),
    postRestaurar: vi.fn(() => of(52)),
  };
  const toast = { sucesso: vi.fn(), erro: vi.fn() };

  /** Abre o modelo da versão com o SolidCraft simulado; `comAlteracoes` é o que ele diz ter sem salvar. */
  async function abrir(comAlteracoes = false) {
    vi.restoreAllMocks();
    [...Object.values(ferramentas), ...Object.values(toast)].forEach((f) => f.mockClear());
    const carregar = vi.spyOn(PonteSolidCraft.prototype, 'carregar').mockResolvedValue();
    const descarregar = vi.spyOn(PonteSolidCraft.prototype, 'descarregar').mockImplementation(() => undefined);
    vi.spyOn(PonteSolidCraft.prototype, 'temAlteracoes').mockResolvedValue(comAlteracoes);
    vi.spyOn(PonteSolidCraft.prototype, 'corposSemMaterial').mockResolvedValue([]);
    const salvar = vi.spyOn(PonteSolidCraft.prototype, 'salvar').mockImplementation((guardar) => guardar(projeto3d));

    TestBed.configureTestingModule({
      imports: [Modelo3dComponent],
      providers: [
        { provide: FerramentasService, useValue: ferramentas },
        { provide: ToastService, useValue: toast },
        { provide: DownloadService, useValue: {} },
        { provide: ProjetosService, useValue: {} },
        { provide: Modelo3dService, useValue: {} },
        { provide: PermissaoService, useValue: { temAlguma: () => true } },
      ],
    });
    TestBed.overrideComponent(Modelo3dComponent, { remove: { imports: [ConversaIaComponent] }, add: { imports: [ConversaFalsaComponent] } });
    const fixture = TestBed.createComponent(Modelo3dComponent);
    fixture.componentRef.setInput('id', '1');
    fixture.componentRef.setInput('versaoId', '20');
    await fixture.whenStable();
    await vi.waitFor(() => expect(ferramentas.getArquivo).toHaveBeenCalledTimes(1));
    await pausa();
    await fixture.whenStable();
    const conversa = fixture.debugElement.query(By.directive(ConversaFalsaComponent)).componentInstance as ConversaFalsaComponent;
    return { fixture, el: fixture.nativeElement as HTMLElement, carregar, descarregar, salvar, conversa };
  }

  /** Deixa as promessas em andamento (a fila de trocas) avançarem. */
  const pausa = () => new Promise((resolve) => setTimeout(resolve));
  const versao = { id: 7 } as VersaoDoArquivoDto;
  const botao = (el: HTMLElement, texto: string) => [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim() === texto);

  /** O SolidCraft de trás demora a abrir: cada `carregar` fica esperando até o teste liberar. */
  function segurarCarregar(carregar: Awaited<ReturnType<typeof abrir>>['carregar']) {
    const liberar: (() => void)[] = [];
    carregar.mockImplementation(() => new Promise<void>((pronto) => liberar.push(pronto)));
    return liberar;
  }

  it('deve abrir o modelo da versão e liberar a prancheta', async () => {
    const { el, carregar } = await abrir();

    expect(carregar).toHaveBeenCalledTimes(1);
    expect(el.querySelector('.sincronizando')).toBeNull();
    expect(botao(el, 'Salvar')!.disabled).toBe(false);
  });

  it('deve restaurar uma vez só no duplo clique', async () => {
    const { fixture, carregar } = await abrir();
    const historico = fixture.debugElement.query(By.directive(HistoricoModeloComponent));

    historico.triggerEventHandler('restaurar', versao);
    historico.triggerEventHandler('restaurar', versao);
    await vi.waitFor(() => expect(toast.sucesso).toHaveBeenCalledWith('Modelo restaurado.'));

    expect(ferramentas.postRestaurar).toHaveBeenCalledTimes(1);
    expect(carregar).toHaveBeenCalledTimes(2);
  });

  it('deve fazer uma troca por vez quando a IA muda o modelo durante outra troca', async () => {
    const { fixture, el, carregar, descarregar, conversa } = await abrir();
    const liberar = segurarCarregar(carregar);

    conversa.arquivoMudou.emit();
    await vi.waitFor(() => expect(liberar.length).toBe(1));
    conversa.arquivoMudou.emit();
    conversa.arquivoMudou.emit();
    await pausa();

    // Só um SolidCraft abre por trás; os outros pedidos esperam.
    expect(liberar.length).toBe(1);

    liberar[0]!();
    await vi.waitFor(() => expect(liberar.length).toBe(2));
    liberar[1]!();
    await pausa();
    await fixture.whenStable();

    expect(liberar.length).toBe(2);
    expect(descarregar).toHaveBeenCalledTimes(2);
    expect(el.querySelector('[role=alert]')).toBeNull();
    expect(botao(el, 'Salvar')!.disabled).toBe(false);
  });

  it('não deve salvar o modelo antigo enquanto o novo abre por trás', async () => {
    const { fixture, el, carregar, salvar, conversa } = await abrir(true);
    const liberar = segurarCarregar(carregar);

    conversa.arquivoMudou.emit();
    await vi.waitFor(() => expect(liberar.length).toBe(1));
    await fixture.whenStable();

    expect(el.textContent).toContain('Atualizando o modelo');
    expect(botao(el, 'Salvar')!.disabled).toBe(true);
    expect(botao(el, 'Peso e materiais')!.disabled).toBe(true);

    // A mensagem à IA espera a troca antes de guardar o que foi feito à mão.
    let enviou = false;
    void conversa.antesDeEnviar()!().then(() => (enviou = true));
    await pausa();
    expect(enviou).toBe(false);
    expect(salvar).not.toHaveBeenCalled();
    expect(ferramentas.putArquivo).not.toHaveBeenCalled();

    liberar[0]!();
    await vi.waitFor(() => expect(enviou).toBe(true));
    await fixture.whenStable();
    expect(botao(el, 'Salvar')!.disabled).toBe(false);
  });

  it('deve avisar e não deixar salvar quando o modelo não abre, e abrir ao tentar de novo', async () => {
    ferramentas.getArquivo.mockReturnValueOnce(throwError(() => new Error('API fora')));
    const { fixture, el, carregar } = await abrir();

    expect(el.querySelector('[role=alert]')!.textContent).toContain('Não foi possível abrir o modelo atual.');
    expect(toast.erro).toHaveBeenCalledWith('Não foi possível abrir o modelo no SolidCraft.');
    expect(botao(el, 'Salvar')!.disabled).toBe(true);
    expect(carregar).not.toHaveBeenCalled();

    botao(el, 'Tentar de novo')!.click();
    await vi.waitFor(() => expect(carregar).toHaveBeenCalledTimes(1));
    await pausa();
    await fixture.whenStable();

    expect(el.querySelector('[role=alert]')).toBeNull();
    expect(botao(el, 'Salvar')!.disabled).toBe(false);
  });

  it('deve confirmar antes de sair com trabalho não salvo e sair direto sem ele', async () => {
    const sujo = await abrir(true);
    const confirmar = vi.spyOn(window, 'confirm').mockReturnValue(false);
    expect(await sujo.fixture.componentInstance.podeSair()).toBe(false);
    expect(confirmar).toHaveBeenCalledTimes(1);

    TestBed.resetTestingModule();
    const limpo = await abrir(false);
    const semPergunta = vi.spyOn(window, 'confirm').mockReturnValue(false);
    expect(await limpo.fixture.componentInstance.podeSair()).toBe(true);
    expect(semPergunta).not.toHaveBeenCalled();
  });

  it('deve trazer o arquivo da outra versão quando a rota troca só o parâmetro', async () => {
    const { fixture, carregar } = await abrir();

    fixture.componentRef.setInput('versaoId', '21');
    await fixture.whenStable();
    await vi.waitFor(() => expect(carregar).toHaveBeenCalledTimes(2));

    expect(ferramentas.getArquivo).toHaveBeenLastCalledWith(1, 21, 'modelo-3d');
  });
});

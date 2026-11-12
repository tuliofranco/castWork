import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { PermissaoService } from '@core/services/permissao.service';
import type { VersaoDoArquivoDto } from '@data/assistente/dto/ferramenta.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { DownloadService } from '@shared/services/download.service';
import { ToastService } from '@shared/services/toast.service';
import { of, throwError } from 'rxjs';

import { ConversaIaComponent } from '../../assistente/conversa-ia.component';
import { ConversaFalsaComponent } from '../../assistente/testing/conversa-falsa.component';

import { Desenho2dComponent } from './desenho-2d.component';
import { HistoricoDesenhoComponent } from './historico-desenho.component';
import { PonteCadCraft } from './ponte-cadcraft';

describe('Desenho2dComponent', () => {
  const dxf = new Blob(['0\nEOF\n']);
  const ferramentas = {
    getArquivo: vi.fn(() => of({ id: 50, nome: 'Desenho', conteudo: dxf })),
    putArquivo: vi.fn(() => of(51)),
    postRestaurar: vi.fn(() => of(52)),
  };
  const toast = { sucesso: vi.fn(), erro: vi.fn() };

  /** Abre o desenho da versão com o CADCraft simulado; `comAlteracoes` é o "•" no título dele. */
  async function abrir(comAlteracoes = false, podeEditar = true) {
    vi.restoreAllMocks();
    [...Object.values(ferramentas), ...Object.values(toast)].forEach((f) => f.mockClear());
    const carregar = vi.spyOn(PonteCadCraft.prototype, 'carregar').mockResolvedValue();
    const descarregar = vi.spyOn(PonteCadCraft.prototype, 'descarregar').mockImplementation(() => undefined);
    vi.spyOn(PonteCadCraft.prototype, 'temAlteracoes').mockReturnValue(comAlteracoes);
    vi.spyOn(PonteCadCraft.prototype, 'salvar').mockResolvedValue(dxf);

    TestBed.configureTestingModule({
      imports: [Desenho2dComponent],
      providers: [
        { provide: FerramentasService, useValue: ferramentas },
        { provide: ToastService, useValue: toast },
        { provide: DownloadService, useValue: {} },
        { provide: ProjetosService, useValue: {} },
        { provide: PermissaoService, useValue: { temAlguma: () => podeEditar } },
      ],
    });
    TestBed.overrideComponent(Desenho2dComponent, { remove: { imports: [ConversaIaComponent] }, add: { imports: [ConversaFalsaComponent] } });
    const fixture = TestBed.createComponent(Desenho2dComponent);
    fixture.componentRef.setInput('id', '1');
    fixture.componentRef.setInput('versaoId', '20');
    await fixture.whenStable();

    fixture.debugElement.query(By.css('iframe')).triggerEventHandler('load');
    await vi.waitFor(() => expect(ferramentas.getArquivo).toHaveBeenCalledTimes(1));
    await pausa();
    await fixture.whenStable();
    const conversa = fixture.debugElement.query(By.directive(ConversaFalsaComponent)).componentInstance as ConversaFalsaComponent;
    return { fixture, el: fixture.nativeElement as HTMLElement, carregar, descarregar, conversa };
  }

  /** Deixa as promessas em andamento (a fila de trocas) avançarem. */
  const pausa = () => new Promise((resolve) => setTimeout(resolve));
  const versao = { id: 7 } as VersaoDoArquivoDto;
  const botao = (el: HTMLElement, texto: string) => [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim() === texto);
  const pedirRestaurar = (fixture: Awaited<ReturnType<typeof abrir>>['fixture']) =>
    fixture.debugElement.query(By.directive(HistoricoDesenhoComponent)).triggerEventHandler('restaurar', versao);

  /** O CADCraft de trás demora a abrir: cada `carregar` fica esperando até o teste liberar. */
  function segurarCarregar(carregar: Awaited<ReturnType<typeof abrir>>['carregar']) {
    const liberar: (() => void)[] = [];
    carregar.mockImplementation(() => new Promise<void>((pronto) => liberar.push(pronto)));
    return liberar;
  }

  async function restaurar(fixture: Awaited<ReturnType<typeof abrir>>['fixture']) {
    pedirRestaurar(fixture);
    await vi.waitFor(() => expect(toast.sucesso).toHaveBeenCalledWith('Desenho restaurado.'));
    await fixture.whenStable();
  }

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
    const { fixture, el } = await abrir();

    await restaurar(fixture);

    expect(ferramentas.putArquivo).not.toHaveBeenCalled();
    expect(el.querySelector('.sincronizando')).toBeNull();
    expect(botao(el, 'Salvar')!.disabled).toBe(false);
  });

  it('deve restaurar uma vez só no duplo clique', async () => {
    const { fixture, carregar } = await abrir();

    pedirRestaurar(fixture);
    pedirRestaurar(fixture);
    await vi.waitFor(() => expect(toast.sucesso).toHaveBeenCalledWith('Desenho restaurado.'));

    expect(ferramentas.postRestaurar).toHaveBeenCalledTimes(1);
    expect(carregar).toHaveBeenCalledTimes(2);
  });

  it('deve fazer uma troca por vez quando a IA muda o desenho durante outra troca', async () => {
    const { fixture, carregar, descarregar, conversa } = await abrir();
    const liberar = segurarCarregar(carregar);

    conversa.arquivoMudou.emit();
    await vi.waitFor(() => expect(liberar.length).toBe(1));
    conversa.arquivoMudou.emit();
    conversa.arquivoMudou.emit();
    await pausa();

    // Só um CADCraft abre por trás; os outros pedidos esperam.
    expect(liberar.length).toBe(1);

    liberar[0]!();
    await vi.waitFor(() => expect(liberar.length).toBe(2));
    liberar[1]!();
    await pausa();
    await fixture.whenStable();

    // Os pedidos que chegaram durante a troca viram uma troca a mais, com o arquivo mais novo, e cada troca libera só
    // o CADCraft que saiu de cena.
    expect(liberar.length).toBe(2);
    expect(descarregar).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.querySelector('[role=alert]')).toBeNull();
    expect(botao(fixture.nativeElement, 'Salvar')!.disabled).toBe(false);
  });

  it('não deve salvar o desenho antigo enquanto o novo abre por trás', async () => {
    const { fixture, el, carregar, conversa } = await abrir(true);
    const liberar = segurarCarregar(carregar);

    conversa.arquivoMudou.emit();
    await vi.waitFor(() => expect(liberar.length).toBe(1));
    await fixture.whenStable();

    expect(el.textContent).toContain('Atualizando o desenho');
    expect(botao(el, 'Salvar')!.disabled).toBe(true);
    expect(botao(el, 'Histórico')!.disabled).toBe(true);

    // A mensagem à IA espera a troca antes de guardar o que foi feito à mão.
    let enviou = false;
    void conversa.antesDeEnviar()!().then(() => (enviou = true));
    await pausa();
    expect(enviou).toBe(false);
    expect(ferramentas.putArquivo).not.toHaveBeenCalled();

    liberar[0]!();
    await vi.waitFor(() => expect(enviou).toBe(true));
    await fixture.whenStable();
    expect(botao(el, 'Salvar')!.disabled).toBe(false);
  });

  it('deve avisar e não deixar salvar quando o desenho não abre, e abrir ao tentar de novo', async () => {
    ferramentas.getArquivo.mockReturnValueOnce(throwError(() => new Error('API fora')));
    const { fixture, el, carregar } = await abrir();

    expect(el.querySelector('[role=alert]')!.textContent).toContain('Não foi possível abrir o desenho atual.');
    expect(toast.erro).toHaveBeenCalledWith('Não foi possível abrir o desenho no CADCraft.');
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

    expect(ferramentas.getArquivo).toHaveBeenLastCalledWith(1, 21, 'desenho-2d');
  });

  it('deve ser só para consulta a quem não pode editar', async () => {
    const { fixture, el, conversa } = await abrir(true, false);

    expect(botao(el, 'Salvar')).toBeUndefined();
    expect(el.textContent).toContain('Seu acesso é só de leitura');
    expect(conversa.somenteLeitura()).toBe(true);

    // Nem a mensagem à IA nem o Restaurar gravam o que foi mudado à mão.
    await conversa.antesDeEnviar()!();
    fixture.debugElement.query(By.css('cw-historico-desenho')).triggerEventHandler('restaurar', versao);
    await pausa();

    expect(ferramentas.putArquivo).not.toHaveBeenCalled();
    expect(ferramentas.postRestaurar).not.toHaveBeenCalled();
  });
});

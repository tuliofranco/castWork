/** A API que o SolidCraft web expõe na própria janela (`window.solidcraft`), documentada em docs/browser-api.md. */
interface ApiSolidCraft {
  request<T = unknown>(metodo: string, parametros?: Record<string, unknown>): Promise<T>;
}

/** O projeto aberto, como `document.export` devolve: o mesmo JSON de um arquivo de projeto. */
interface ProjetoExportado {
  readonly name: string;
  readonly revision: number;
  readonly dirty: boolean;
  readonly text: string;
}

type JanelaSolidCraft = Window & typeof globalThis & { solidcraft?: ApiSolidCraft };

/**
 * Ponte com o SolidCraft web dentro do iframe (mesma origem: servido pelo próprio front em /solidcraft, no
 * modo `?embed=editor`, que não guarda nada no navegador). O SolidCraft tem API para a página que o embute:
 * - abrir: `document.import` com o JSON do projeto;
 * - salvar: `document.export` devolve o JSON e, guardado no Castwork, `document.saved` marca o projeto salvo.
 * O ⌘S/Ctrl+S do joalheiro dentro do SolidCraft também vai para o Castwork.
 */
export class PonteSolidCraft {
  constructor(
    private readonly iframe: HTMLIFrameElement,
    /** Chamado quando o joalheiro aperta ⌘S/Ctrl+S dentro do SolidCraft. */
    private readonly aoSalvarPeloJoalheiro: () => void,
  ) {}

  /**
   * Recarrega o SolidCraft (um projeto só, sem acumular) e abre o projeto, ou fica no projeto em branco
   * quando não há arquivo.
   */
  async carregar(arquivo: Blob | null, nome = 'modelo'): Promise<void> {
    const api = await this.recarregar();
    if (!arquivo) return;
    await api.request('document.import', { text: await arquivo.text(), name: nome });
  }

  /** O projeto tem alterações que ainda não foram para o Castwork. */
  async temAlteracoes(): Promise<boolean> {
    const api = this.janela().solidcraft;
    if (!api) return false;
    return (await api.request<ProjetoExportado>('document.export')).dirty;
  }

  /**
   * Exporta o projeto aberto (JSON), entrega a quem guarda e, guardado, avisa o SolidCraft de que essa
   * revisão está salva. Se o joalheiro mexeu no meio do caminho, o SolidCraft recusa e o projeto segue com
   * alterações, como deve.
   */
  async salvar(guardar: (arquivo: Blob) => Promise<void>): Promise<void> {
    const api = this.api();
    const projeto = await api.request<ProjetoExportado>('document.export');
    await guardar(new Blob([projeto.text], { type: 'application/json' }));
    await api.request('document.saved', { revision: projeto.revision }).catch(() => undefined);
  }

  /** Libera o SolidCraft deste iframe (memória do WebAssembly) quando ele sai de cena. */
  descarregar(): void {
    this.iframe.src = 'about:blank';
  }

  /** Recarrega o SolidCraft, ou o carrega pela primeira vez num iframe ainda vazio, e espera a API. */
  private async recarregar(): Promise<ApiSolidCraft> {
    await new Promise<void>((resolve) => {
      this.iframe.addEventListener('load', () => resolve(), { once: true });
      if (this.iframe.contentWindow?.location.pathname.startsWith('/solidcraft')) this.iframe.contentWindow.location.reload();
      else this.iframe.src = '/solidcraft/?embed=editor';
    });
    const janela = this.janela();
    this.capturarAtalhoDeSalvar(janela);
    // O app terminou de subir quando a API responde pelo projeto em branco.
    await this.aguardar(async () => !!janela.solidcraft && (await janela.solidcraft.request('document.export').then(() => true, () => false)), 60_000);
    return this.api();
  }

  /** O ⌘S/Ctrl+S é do Castwork: chega antes do SolidCraft (fase de captura) e não vira download. */
  private capturarAtalhoDeSalvar(janela: JanelaSolidCraft): void {
    janela.addEventListener(
      'keydown',
      (evento) => {
        if ((evento.metaKey || evento.ctrlKey) && !evento.shiftKey && evento.key.toLowerCase() === 's') {
          evento.preventDefault();
          evento.stopImmediatePropagation();
          this.aoSalvarPeloJoalheiro();
        }
      },
      { capture: true },
    );
  }

  private api(): ApiSolidCraft {
    const api = this.janela().solidcraft;
    if (!api) throw new Error('SolidCraft indisponível');
    return api;
  }

  private janela(): JanelaSolidCraft {
    const janela = this.iframe.contentWindow as JanelaSolidCraft | null;
    if (!janela) throw new Error('SolidCraft indisponível');
    return janela;
  }

  private async aguardar(condicao: () => Promise<boolean>, limite: number): Promise<void> {
    const inicio = Date.now();
    while (!(await condicao())) {
      if (Date.now() - inicio > limite) throw new Error('SolidCraft não respondeu a tempo.');
      await new Promise((resolve) => setTimeout(resolve, 200));
    }
  }
}

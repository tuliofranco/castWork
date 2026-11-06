/**
 * Ponte com o CADCraft web dentro do iframe (mesma origem: servido pelo próprio front em /cadcraft).
 * O CADCraft não tem API para a página que o embute, então a ponte usa o que um usuário faria:
 * - abrir: solta o arquivo no canvas (o CADCraft abre arquivos arrastados);
 * - salvar: aperta ⌘S/Ctrl+S e captura o arquivo que o CADCraft entregaria como download.
 * Todo salvamento (o nosso e o do joalheiro, pelo menu ou atalho) vai para o Castwork em vez de baixar.
 */
export class PonteCadCraft {
  private aoSalvar: ((arquivo: Blob) => void) | null = null;
  private esperandoSalvar: ((arquivo: Blob) => void) | null = null;

  constructor(
    private readonly iframe: HTMLIFrameElement,
    /** Chamado quando o joalheiro salva no próprio CADCraft (⌘S ou Arquivo ▸ Salvar). */
    aoSalvarPeloJoalheiro: (arquivo: Blob) => void,
  ) {
    this.aoSalvar = aoSalvarPeloJoalheiro;
  }

  /**
   * Recarrega o CADCraft (uma aba só, sem acumular desenhos) e abre o arquivo, ou fica no desenho em
   * branco quando não há arquivo.
   */
  async carregar(arquivo: Blob | null, nome = 'desenho.dxf'): Promise<void> {
    const janela = await this.recarregar();
    this.instalarCaptura(janela);
    if (!arquivo) return;

    const canvas = await this.canvas(janela);
    const dados = new janela.DataTransfer();
    dados.items.add(new janela.File([await arquivo.arrayBuffer()], nome));
    const { left, top, width, height } = canvas.getBoundingClientRect();
    const ponto = { clientX: left + width / 2, clientY: top + height / 2 };
    for (const tipo of ['dragenter', 'dragover', 'drop']) {
      canvas.dispatchEvent(new janela.DragEvent(tipo, { dataTransfer: dados, bubbles: true, cancelable: true, ...ponto }));
    }
    await this.aguardar(() => janela.document.title.startsWith(nome), 10_000);
  }

  /** O desenho tem alterações que ainda não foram para o Castwork (o CADCraft marca o título com •). */
  temAlteracoes(): boolean {
    return this.iframe.contentDocument?.title.includes('•') ?? false;
  }

  /** Salva o desenho aberto e devolve o arquivo (DXF) sem baixar nada. */
  async salvar(): Promise<Blob> {
    const janela = this.janela();
    const canvas = await this.canvas(janela);
    const salvo = new Promise<Blob>((resolve, reject) => {
      this.esperandoSalvar = resolve;
      setTimeout(() => reject(new Error('O CADCraft não salvou o desenho.')), 10_000);
    });

    const mac = /Mac|iPad|iPhone/.test(janela.navigator.userAgent);
    canvas.focus();
    for (const tipo of ['keydown', 'keyup']) {
      const tecla = new janela.KeyboardEvent(tipo, { key: 's', code: 'KeyS', metaKey: mac, ctrlKey: !mac, bubbles: true, cancelable: true });
      janela.document.dispatchEvent(tecla);
    }
    return salvo;
  }

  /**
   * O CADCraft salva criando um blob e clicando num link de download. A ponte guarda o blob e cancela o
   * download: o arquivo vai para quem pediu (salvar()) ou para o callback do joalheiro.
   */
  private instalarCaptura(janela: Window & typeof globalThis): void {
    let ultimoBlob: Blob | null = null;
    const criarUrl = janela.URL.createObjectURL.bind(janela.URL);
    janela.URL.createObjectURL = (objeto: Blob | MediaSource) => {
      if (objeto instanceof janela.Blob) ultimoBlob = objeto;
      return criarUrl(objeto);
    };

    // eslint-disable-next-line @typescript-eslint/no-this-alias -- o `this` do clique é o link
    const ponte = this;
    const original = janela.HTMLAnchorElement.prototype.click;
    janela.HTMLAnchorElement.prototype.click = function (this: HTMLAnchorElement) {
      if (this.download && /\.(dxf|dwg)$/i.test(this.download) && ultimoBlob) {
        const arquivo = ultimoBlob;
        ultimoBlob = null;
        const destino = ponte.esperandoSalvar ?? ponte.aoSalvar;
        ponte.esperandoSalvar = null;
        destino?.(arquivo);
        return;
      }
      original.call(this);
    };
  }

  private async recarregar(): Promise<Window & typeof globalThis> {
    await new Promise<void>((resolve) => {
      this.iframe.addEventListener('load', () => resolve(), { once: true });
      this.iframe.contentWindow?.location.reload();
    });
    const janela = this.janela();
    await this.canvas(janela);
    // O app terminou de subir quando dá nome ao desenho em branco.
    await this.aguardar(() => /Drawing\d/.test(janela.document.title), 30_000);
    return janela;
  }

  private janela(): Window & typeof globalThis {
    const janela = this.iframe.contentWindow as (Window & typeof globalThis) | null;
    if (!janela) throw new Error('CADCraft indisponível');
    return janela;
  }

  private async canvas(janela: Window & typeof globalThis): Promise<HTMLCanvasElement> {
    await this.aguardar(() => !!janela.document.getElementById('cadcraft_canvas'), 30_000);
    return janela.document.getElementById('cadcraft_canvas') as HTMLCanvasElement;
  }

  private aguardar(condicao: () => boolean, limite: number): Promise<void> {
    return new Promise((resolve, reject) => {
      const inicio = Date.now();
      const verificar = () => {
        if (condicao()) resolve();
        else if (Date.now() - inicio > limite) reject(new Error('CADCraft não respondeu a tempo.'));
        else setTimeout(verificar, 100);
      };
      verificar();
    });
  }
}

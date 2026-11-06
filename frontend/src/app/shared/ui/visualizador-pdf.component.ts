import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, effect, inject, input, signal, viewChild } from '@angular/core';

/** Escala máxima ao renderizar: nítido em tela retina sem estourar memória no tablet. */
const PIXELS_POR_PONTO_MAXIMO = 3;

/**
 * Mostra um PDF página a página com o PDF.js, igual em iPad, Android e computador (o leitor nativo em
 * iframe é inconsistente no Safari do iPad). A biblioteca só é baixada quando o visualizador aparece.
 */
@Component({
  selector: 'cw-visualizador-pdf',
  template: `
    @if (erro()) {
      <p class="falha" role="alert">Não foi possível mostrar o PDF.</p>
    } @else if (carregando()) {
      <p class="carregando" role="status"><span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Montando a pré-visualização...</p>
    }
    <div #paginas class="paginas" [attr.aria-label]="rotulo()" role="document"></div>
  `,
  styles: `
    :host {
      display: block;
    }

    .paginas {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
    }

    .paginas ::ng-deep canvas {
      display: block;
      width: 100%;
      max-width: 52rem;
      height: auto;
      border-radius: var(--cw-radius-sm);
      background: #fff;
      box-shadow: 0 1px 3px oklch(0.2 0.01 60 / 0.25);
    }

    .carregando,
    .falha {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      justify-content: center;
      margin: 2rem 0;
      color: var(--cw-ink-muted);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VisualizadorPdfComponent {
  readonly arquivo = input.required<Blob>();
  readonly rotulo = input('Pré-visualização do documento');

  protected readonly carregando = signal(true);
  protected readonly erro = signal(false);

  private readonly paginas = viewChild.required<ElementRef<HTMLElement>>('paginas');
  private destruido = false;

  constructor() {
    inject(DestroyRef).onDestroy(() => (this.destruido = true));
    effect(() => void this.renderizar(this.arquivo()));
  }

  private async renderizar(arquivo: Blob): Promise<void> {
    this.carregando.set(true);
    this.erro.set(false);
    const alvo = this.paginas().nativeElement;
    alvo.replaceChildren();
    let tarefa: { destroy(): Promise<void> } | undefined;
    try {
      const pdfjs = await import('pdfjs-dist');
      pdfjs.GlobalWorkerOptions.workerSrc = '/pdfjs/pdf.worker.min.mjs';
      const carregamento = pdfjs.getDocument({ data: new Uint8Array(await arquivo.arrayBuffer()) });
      tarefa = carregamento;
      const documento = await carregamento.promise;
      const escala = Math.min(window.devicePixelRatio || 1, PIXELS_POR_PONTO_MAXIMO) * 1.5;
      for (let numero = 1; numero <= documento.numPages && !this.destruido; numero++) {
        const pagina = await documento.getPage(numero);
        const viewport = pagina.getViewport({ scale: escala });
        const canvas = document.createElement('canvas');
        canvas.width = viewport.width;
        canvas.height = viewport.height;
        canvas.setAttribute('aria-label', `Página ${numero} de ${documento.numPages}`);
        alvo.append(canvas);
        await pagina.render({ canvas, viewport }).promise;
      }
    } catch {
      this.erro.set(true);
    } finally {
      // Libera o worker e a memória das páginas; os canvas já desenhados continuam na tela.
      await tarefa?.destroy();
      this.carregando.set(false);
    }
  }
}

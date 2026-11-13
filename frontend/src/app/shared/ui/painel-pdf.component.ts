import { ChangeDetectionStrategy, Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

import { VisualizadorPdfComponent } from './visualizador-pdf.component';

/**
 * PDF em tela cheia, com a barra de ações sempre à mão no topo. Abre quando recebe um arquivo e avisa
 * em `fechado` (botão, Esc ou toque fora): quem usa só limpa o arquivo. As ações vêm por projeção.
 */
let paineis = 0;

@Component({
  selector: 'cw-painel-pdf',
  imports: [VisualizadorPdfComponent],
  template: `
    <dialog #dialogo class="painel-pdf" [attr.aria-labelledby]="idTitulo" (close)="fechado.emit()">
      @if (arquivo(); as pdf) {
        <header class="barra-pdf">
          <h2 [id]="idTitulo">{{ titulo() }}</h2>
          <div class="acoes">
            <ng-content />
            <button type="button" class="btn btn-sutil btn-icone" aria-label="Fechar" (click)="dialogo.close()"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
          </div>
        </header>
        <div class="folha">
          <cw-visualizador-pdf [arquivo]="pdf" [rotulo]="titulo()" />
        </div>
      }
    </dialog>
  `,
  styles: `
    .painel-pdf {
      width: 100vw;
      max-width: 100vw;
      height: 100dvh;
      max-height: 100dvh;
      margin: 0;
      padding: 0;
      border: 0;
      background: var(--cw-surface-2);
      color: var(--cw-ink);

      &::backdrop {
        background: oklch(0.15 0.01 60 / 0.6);
      }

      &[open] {
        display: flex;
        flex-direction: column;
      }
    }

    .barra-pdf {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 0.75rem 1.25rem;
      border-bottom: 1px solid var(--cw-border);
      background: var(--cw-surface);

      h2 {
        margin: 0;
        font-size: 1.0625rem;
        font-weight: 600;
      }
    }

    .acoes {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;

      ::ng-deep .btn {
        min-height: var(--cw-alvo);
      }
    }

    .folha {
      flex: 1 1 auto;
      overflow: auto;
      padding: 1.5rem 1rem 3rem;
      -webkit-overflow-scrolling: touch;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PainelPdfComponent {
  /** Pode haver mais de um painel na página: cada título tem o próprio id. */
  protected readonly idTitulo = `titulo-pdf-${++paineis}`;
  readonly arquivo = input<Blob | null>(null);
  readonly titulo = input.required<string>();
  readonly fechado = output();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  constructor() {
    // Diálogo nativo: prende o foco, fecha com Esc e devolve o foco ao botão que abriu.
    effect(() => {
      const dialogo = this.dialogo().nativeElement;
      if (this.arquivo() && !dialogo.open) dialogo.showModal();
      else if (!this.arquivo() && dialogo.open) dialogo.close();
    });
  }
}

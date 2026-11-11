import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

/**
 * No desenho 2D e no modelo 3D de uma peça do projeto **publicada**: trocar o arquivo final faz dela outra peça, que volta
 * a rascunho e perde o vínculo com a biblioteca (salvar, restaurar ou a IA alterar sem marcar como final não muda). Mostra
 * o aviso fixo por cima da prancheta e, antes de marcar o final, pede a confirmação (`confirmar()`), uma vez por visita.
 */
@Component({
  selector: 'cw-aviso-peca-publicada',
  template: `
    @if (pedindo()) {
      <div class="confirmar" role="alertdialog" aria-labelledby="texto-outra-peca">
        <span id="texto-outra-peca">
          Esta peça está publicada na biblioteca. Trocar o arquivo final faz dela outra peça: ela volta a rascunho e, publicada de novo, entra na
          biblioteca como peça nova. A peça da biblioteca não muda.
        </span>
        <span class="botoes">
          <button type="button" class="btn btn-sutil" (click)="responder(false)">Cancelar</button>
          <button type="button" class="btn btn-primary" (click)="responder(true)">Continuar como outra peça</button>
        </span>
      </div>
    } @else if (publicada() && !confirmado()) {
      <p class="aviso" role="status">
        <i class="bi bi-cloud-check" aria-hidden="true"></i> Peça publicada: salvar não muda a peça; marcar outro arquivo como final faz dela outra peça.
      </p>
    }
  `,
  styles: `
    :host { position: absolute; right: 0; bottom: 0.75rem; left: 0; z-index: 4; display: flex; justify-content: center; padding: 0 1rem; pointer-events: none; }
    .aviso {
      display: flex; align-items: center; gap: 0.5rem; margin: 0; padding: 0.375rem 0.875rem; border-radius: 999px;
      background: rgb(20 24 28 / 0.88); color: #e6e6e6; font-size: 0.8125rem;
    }
    .confirmar {
      display: flex; flex-wrap: wrap; align-items: center; gap: 0.75rem; max-width: 46rem; padding: 0.875rem 1rem;
      border-radius: var(--cw-radius); background: var(--cw-warning-soft); color: var(--cw-warning-ink); pointer-events: auto;
    }
    .botoes { display: flex; gap: 0.5rem; margin-left: auto; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvisoPecaPublicadaComponent {
  /** A peça está publicada na biblioteca (fora da peça, ou em rascunho, o aviso não aparece). */
  readonly publicada = input(false);

  protected readonly pedindo = signal(false);
  /** Já confirmou nesta visita: não pergunta de novo a cada arquivo final. */
  protected readonly confirmado = signal(false);
  private resolver: ((sim: boolean) => void) | null = null;

  /** `true` quando pode marcar o final: a peça não está publicada, ou o joalheiro aceitou que ela vire outra peça. */
  confirmar(): Promise<boolean> {
    if (!this.publicada() || this.confirmado()) return Promise.resolve(true);
    this.resolver?.(false);
    this.pedindo.set(true);
    return new Promise<boolean>((resolve) => (this.resolver = resolve));
  }

  protected responder(sim: boolean): void {
    this.pedindo.set(false);
    if (sim) this.confirmado.set(true);
    this.resolver?.(sim);
    this.resolver = null;
  }
}

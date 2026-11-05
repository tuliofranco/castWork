import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/** Rodapé de listagem paginada por cursor: total, página atual e anterior/próxima. */
@Component({
  selector: 'cw-paginacao',
  template: `
    <span class="resumo">{{ resumo() }}</span>
    @if (temAnterior() || temProxima()) {
      <div class="botoes">
        <button type="button" class="btn btn-secundario" [disabled]="!temAnterior()" (click)="anterior.emit()">
          <i class="bi bi-chevron-left" aria-hidden="true"></i> Anterior
        </button>
        <button type="button" class="btn btn-secundario" [disabled]="!temProxima()" (click)="proxima.emit()">
          Próxima <i class="bi bi-chevron-right" aria-hidden="true"></i>
        </button>
      </div>
    }
  `,
  styles: `
    :host {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.75rem;
      margin-top: 1rem;
    }
    .resumo {
      margin-right: auto;
      color: var(--cw-ink-muted);
      font-size: 0.875rem;
    }
    .botoes {
      display: flex;
      gap: 0.5rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginacaoComponent {
  readonly total = input.required<number>();
  readonly pagina = input.required<number>();
  readonly singular = input.required<string>();
  readonly plural = input.required<string>();
  readonly temAnterior = input(false);
  readonly temProxima = input(false);

  readonly anterior = output();
  readonly proxima = output();

  protected readonly resumo = computed(() => {
    const total = this.total();
    const texto = `${total} ${total === 1 ? this.singular() : this.plural()}`;
    return this.pagina() > 1 || this.temProxima() ? `${texto} · página ${this.pagina()}` : texto;
  });
}

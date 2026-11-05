import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type TomSelo = 'neutro' | 'destaque' | 'sucesso' | 'aviso' | 'perigo';

/** Selo de status. Sempre com ícone e texto: o estado nunca depende só da cor. */
@Component({
  selector: 'cw-selo',
  template: `
    @if (icone()) {
      <i class="bi" [class]="'bi-' + icone()" aria-hidden="true"></i>
    }
    <ng-content />
  `,
  host: { '[class]': '"tom-" + tom()' },
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 0.3rem;
      padding: 0.25rem 0.625rem;
      border-radius: 999px;
      font-size: 0.8125rem;
      font-weight: 600;
      line-height: 1.2;
      white-space: nowrap;
    }
    :host(.tom-neutro) {
      background: var(--cw-surface-2);
      color: var(--cw-ink-muted);
    }
    :host(.tom-destaque) {
      background: var(--cw-accent-soft);
      color: var(--cw-accent-ink);
    }
    :host(.tom-sucesso) {
      background: var(--cw-success-soft);
      color: var(--cw-success-ink);
    }
    :host(.tom-aviso) {
      background: var(--cw-warning-soft);
      color: var(--cw-warning-ink);
    }
    :host(.tom-perigo) {
      background: var(--cw-danger-soft);
      color: var(--cw-danger-ink);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeloComponent {
  readonly tom = input<TomSelo>('neutro');
  readonly icone = input<string>();
}

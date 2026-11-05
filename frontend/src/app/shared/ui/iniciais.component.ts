import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Avatar com as iniciais do nome (primeira e última palavra). */
@Component({
  selector: 'cw-iniciais',
  template: '{{ iniciais() }}',
  host: { 'aria-hidden': 'true' },
  styles: `
    :host {
      display: inline-grid;
      place-items: center;
      flex: none;
      width: 2.5rem;
      height: 2.5rem;
      border-radius: 50%;
      background: var(--cw-accent-soft);
      color: var(--cw-accent-ink);
      font-weight: 650;
      font-size: 0.875rem;
      letter-spacing: 0.02em;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IniciaisComponent {
  readonly nome = input.required<string>();

  protected readonly iniciais = computed(() => {
    // Só palavras que começam com letra: "Joana Ourives (QA)" vira JO, não J(.
    const partes = this.nome().trim().split(/\s+/).filter((parte) => /^\p{L}/u.test(parte));
    if (partes.length === 0) return '?';
    const primeira = partes.at(0)?.charAt(0) ?? '';
    const ultima = partes.length > 1 ? (partes.at(-1)?.charAt(0) ?? '') : '';
    return (primeira + ultima).toUpperCase();
  });
}

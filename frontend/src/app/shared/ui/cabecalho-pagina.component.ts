import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Topo de toda página: voltar (opcional), título em Fraunces, subtítulo e ações à direita.
 * Ações entram por `<ng-content select="[acoes]">`.
 */
@Component({
  selector: 'cw-cabecalho-pagina',
  imports: [RouterLink],
  template: `
    @if (voltarPara(); as destino) {
      <a class="voltar" [routerLink]="destino"><i class="bi bi-chevron-left" aria-hidden="true"></i>{{ voltarRotulo() }}</a>
    }
    <div class="linha">
      <div class="textos">
        <h1>{{ titulo() }}</h1>
        @if (subtitulo()) {
          <p class="subtitulo">{{ subtitulo() }}</p>
        }
        <ng-content select="[detalhes]" />
      </div>
      <div class="acoes"><ng-content select="[acoes]" /></div>
    </div>
  `,
  styles: `
    :host {
      display: block;
      margin-bottom: 1.75rem;
    }
    .voltar {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      min-height: 2.75rem;
      margin: -0.5rem 0 0.25rem -0.375rem;
      padding: 0 0.375rem;
      border-radius: var(--cw-radius-sm);
      color: var(--cw-ink-muted);
      font-weight: 600;
      font-size: 0.875rem;
    }
    .voltar:hover {
      color: var(--cw-ink);
      background: var(--cw-surface-hover);
    }
    .linha {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      gap: 1rem;
    }
    .textos {
      flex: 1 1 18rem;
      min-width: 0;
    }
    h1 {
      margin: 0;
    }
    .subtitulo {
      margin: 0.375rem 0 0;
      max-width: 68ch;
      color: var(--cw-ink-muted);
    }
    .acoes {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.5rem;
    }
    .acoes:empty {
      display: none;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CabecalhoPaginaComponent {
  readonly titulo = input.required<string>();
  readonly subtitulo = input<string>();
  readonly voltarPara = input<string>();
  readonly voltarRotulo = input('Voltar');
}

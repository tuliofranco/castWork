import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Página genérica; `codigo` e `mensagem` chegam do `data` da rota via component input binding. */
@Component({
  selector: 'cw-pagina-erro',
  imports: [RouterLink],
  template: `
    <div class="pagina">
      <p class="codigo">{{ codigo() }}</p>
      <h1>{{ mensagem() }}</h1>
      <a routerLink="/" class="btn btn-primary btn-lg">Voltar ao início</a>
    </div>
  `,
  styles: `
    .pagina { display: grid; justify-items: center; gap: 1rem; text-align: center; padding: 2rem; }
    .codigo { margin: 0; font-family: var(--cw-font-titulo); font-size: 4.5rem; line-height: 1; color: var(--cw-ink-faint); }
    h1 { max-width: 28ch; margin: 0 0 0.5rem; font-size: 1.375rem; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaErroComponent {
  readonly codigo = input.required<string>();
  readonly mensagem = input.required<string>();
}

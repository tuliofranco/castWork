import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Página genérica; `codigo` e `mensagem` chegam do `data` da rota via component input binding. */
@Component({
  selector: 'cw-pagina-erro',
  imports: [RouterLink],
  template: `
    <div class="text-center">
      <h1 class="display-1 fw-bold">{{ codigo() }}</h1>
      <p class="lead">{{ mensagem() }}</p>
      <a routerLink="/" class="btn btn-primary">Voltar ao início</a>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaErroComponent {
  readonly codigo = input.required<string>();
  readonly mensagem = input.required<string>();
}

import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { AuthService } from '@core/services/auth.service';
import { RouterLink } from '@angular/router';

/** Página genérica; `codigo` e `mensagem` chegam do `data` da rota via component input binding. */
@Component({
  selector: 'cw-pagina-erro',
  imports: [RouterLink],
  template: `
    <div class="pagina">
      <p class="codigo">{{ codigo() }}</p>
      <h1>{{ mensagem() }}</h1>
      <div class="acoes">
        <a routerLink="/" class="btn btn-primary btn-lg">Voltar ao início</a>
        @if (auth.isLoggedIn()) {
          <!-- Quem não pode ver nenhuma página não tem menu: precisa de uma saída daqui. -->
          <button type="button" class="btn btn-sutil btn-lg" (click)="auth.logout()">Sair</button>
        }
      </div>
    </div>
  `,
  styles: `
    .pagina { display: grid; justify-items: center; gap: 1rem; text-align: center; padding: 2rem; }
    .codigo { margin: 0; font-family: var(--cw-font-titulo); font-size: 4.5rem; line-height: 1; color: var(--cw-ink-faint); }
    h1 { max-width: 28ch; margin: 0 0 0.5rem; font-size: 1.375rem; }
    .acoes { display: flex; flex-wrap: wrap; justify-content: center; gap: 0.75rem; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginaErroComponent {
  protected readonly auth = inject(AuthService);

  readonly codigo = input.required<string>();
  readonly mensagem = input.required<string>();
}

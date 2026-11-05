import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Envolve uma listagem e decide o que mostrar: erro (com tentar de novo), esqueleto na primeira carga,
 * estado vazio que ensina o próximo passo, ou o conteúdo. Recarregamentos mantêm o conteúdo na tela.
 */
@Component({
  selector: 'cw-estado-lista',
  template: `
    @if (erro()) {
      <div class="aviso erro" role="alert">
        <i class="bi bi-cloud-slash" aria-hidden="true"></i>
        <div>
          <p class="titulo">{{ mensagemErro() }}</p>
          <p>Verifique a conexão e tente de novo.</p>
        </div>
        <button type="button" class="btn btn-secundario" (click)="tentarDeNovo.emit()">Tentar de novo</button>
      </div>
    } @else if (carregando()) {
      <div class="esqueleto" aria-busy="true" aria-label="Carregando">
        @for (linha of linhas(); track linha) {
          <div class="barra"><span></span><span></span></div>
        }
      </div>
    } @else if (vazio()) {
      <div class="vazio">
        <span class="icone"><i class="bi" [class]="'bi-' + iconeVazio()" aria-hidden="true"></i></span>
        <p class="titulo">{{ tituloVazio() }}</p>
        @if (descricaoVazio()) {
          <p class="descricao">{{ descricaoVazio() }}</p>
        }
        <ng-content select="[acaoVazio]" />
      </div>
    } @else {
      <ng-content />
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .titulo {
      font-weight: 600;
      margin: 0;
    }
    .aviso {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 1rem;
      padding: 1.25rem;
      border-radius: var(--cw-radius-lg);
      background: var(--cw-danger-soft);
      color: var(--cw-danger-ink);
    }
    .aviso > i {
      font-size: 1.5rem;
    }
    .aviso div {
      flex: 1 1 14rem;
    }
    .aviso p {
      margin: 0;
    }
    .esqueleto {
      display: grid;
      gap: 0.5rem;
    }
    .barra {
      display: grid;
      gap: 0.5rem;
      padding: 1.125rem 1.25rem;
      border-radius: var(--cw-radius);
      background: var(--cw-surface);
      border: 1px solid var(--cw-border);
    }
    .barra span {
      display: block;
      height: 0.75rem;
      border-radius: 999px;
      background: var(--cw-surface-2);
      animation: pulsar 1.4s var(--cw-ease) infinite alternate;
    }
    .barra span:first-child {
      width: 38%;
    }
    .barra span:last-child {
      width: 22%;
      height: 0.625rem;
    }
    @keyframes pulsar {
      to {
        opacity: 0.45;
      }
    }
    .vazio {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      gap: 0.5rem;
      padding: 3.5rem 1.5rem;
      border: 1px dashed var(--cw-border-strong);
      border-radius: var(--cw-radius-lg);
    }
    .icone {
      display: grid;
      place-items: center;
      width: 3.5rem;
      height: 3.5rem;
      margin-bottom: 0.5rem;
      border-radius: 50%;
      background: var(--cw-accent-soft);
      color: var(--cw-accent-ink);
      font-size: 1.5rem;
    }
    .descricao {
      max-width: 44ch;
      margin: 0 0 0.75rem;
      color: var(--cw-ink-muted);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EstadoListaComponent {
  readonly carregando = input(false);
  readonly erro = input(false);
  readonly vazio = input(false);
  readonly mensagemErro = input('Não foi possível carregar.');
  readonly iconeVazio = input('inbox');
  readonly tituloVazio = input('Nada por aqui ainda.');
  readonly descricaoVazio = input<string>();
  readonly quantidadeEsqueleto = input(4);

  readonly tentarDeNovo = output();

  protected linhas(): number[] {
    return Array.from({ length: this.quantidadeEsqueleto() }, (_, i) => i);
  }
}

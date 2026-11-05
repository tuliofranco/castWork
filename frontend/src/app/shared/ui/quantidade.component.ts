import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, linkedSignal, output } from '@angular/core';

/**
 * Stepper − / + para toque, com campo editável. Atualiza na hora na tela e só emite `alterado`
 * depois de uma pausa (vários toques seguidos viram uma única gravação).
 */
@Component({
  selector: 'cw-quantidade',
  template: `
    <button type="button" class="btn btn-secundario" [disabled]="desabilitado() || atual() <= minimo()" [attr.aria-label]="'Diminuir ' + rotulo()" (click)="somar(-1)">
      <i class="bi bi-dash-lg" aria-hidden="true"></i>
    </button>
    <input
      type="number"
      inputmode="numeric"
      class="form-control"
      [min]="minimo()"
      [max]="maximo()"
      [value]="atual()"
      [disabled]="desabilitado()"
      [attr.aria-label]="'Quantidade de ' + rotulo()"
      (change)="digitar($any($event.target).value)"
    />
    <button type="button" class="btn btn-secundario" [disabled]="desabilitado() || atual() >= maximo()" [attr.aria-label]="'Aumentar ' + rotulo()" (click)="somar(1)">
      <i class="bi bi-plus-lg" aria-hidden="true"></i>
    </button>
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: stretch;
    }
    .btn {
      width: var(--cw-alvo);
      min-height: var(--cw-alvo);
      padding: 0;
    }
    .btn:first-child {
      border-top-right-radius: 0;
      border-bottom-right-radius: 0;
    }
    .btn:last-child {
      border-top-left-radius: 0;
      border-bottom-left-radius: 0;
    }
    input {
      width: 4.25rem;
      border-radius: 0;
      border-left: 0;
      border-right: 0;
      text-align: center;
      font-variant-numeric: tabular-nums;
      font-weight: 600;
      -moz-appearance: textfield;
    }
    input::-webkit-inner-spin-button,
    input::-webkit-outer-spin-button {
      appearance: none;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuantidadeComponent {
  readonly valor = input.required<number>();
  readonly rotulo = input('item');
  readonly minimo = input(1);
  readonly maximo = input(10_000);
  readonly desabilitado = input(false);
  /** Tempo sem toques antes de emitir (ms). Zero emite na hora. */
  readonly espera = input(500);

  readonly alterado = output<number>();

  protected readonly atual = linkedSignal(() => this.valor());
  private temporizador: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.temporizador));
  }

  protected somar(delta: number): void {
    this.definir(this.atual() + delta);
  }

  protected digitar(texto: string): void {
    const numero = Math.trunc(Number(texto));
    this.definir(Number.isFinite(numero) ? numero : this.valor());
  }

  private definir(valor: number): void {
    const limitado = Math.min(this.maximo(), Math.max(this.minimo(), valor));
    this.atual.set(limitado);
    clearTimeout(this.temporizador);
    const emitir = () => {
      if (this.atual() !== this.valor()) this.alterado.emit(this.atual());
    };
    if (this.espera() === 0) emitir();
    else this.temporizador = setTimeout(emitir, this.espera());
  }

  /** Valor mostrado agora (pode estar à frente do `valor` até a emissão). */
  valorAtual(): number {
    return this.atual();
  }
}

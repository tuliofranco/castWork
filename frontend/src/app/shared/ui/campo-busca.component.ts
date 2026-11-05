import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { type FormControl, ReactiveFormsModule } from '@angular/forms';

/** Busca com ícone e botão de limpar (aparece só quando há texto). O debounce fica com a página. */
@Component({
  selector: 'cw-campo-busca',
  imports: [ReactiveFormsModule],
  template: `
    <i class="bi bi-search lupa" aria-hidden="true"></i>
    <input type="search" class="form-control" [formControl]="controle()" [placeholder]="placeholder()" [attr.aria-label]="rotulo()" />
    @if (controle().value) {
      <button type="button" class="btn btn-sutil btn-icone limpar" aria-label="Limpar busca" (click)="controle().setValue('')">
        <i class="bi bi-x-lg" aria-hidden="true"></i>
      </button>
    }
  `,
  styles: `
    :host {
      position: relative;
      display: block;
      flex: 1 1 16rem;
      max-width: 26rem;
    }
    .lupa {
      position: absolute;
      left: 0.95rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--cw-ink-faint);
      pointer-events: none;
    }
    input {
      padding-left: 2.5rem;
      padding-right: 3rem;
    }
    input::-webkit-search-cancel-button {
      appearance: none;
    }
    .limpar {
      position: absolute;
      right: 0.125rem;
      top: 50%;
      transform: translateY(-50%);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampoBuscaComponent {
  readonly controle = input.required<FormControl<string>>();
  readonly placeholder = input('Buscar');
  readonly rotulo = input('Buscar');
}

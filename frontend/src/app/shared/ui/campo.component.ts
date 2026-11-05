import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import type { AbstractControl } from '@angular/forms';
import { map, startWith, switchMap } from 'rxjs';

/**
 * Rótulo, "opcional", dica e mensagem de erro em volta de um controle projetado.
 * Erro aparece depois de tocado (ou vindo do servidor, chave `servidor`); `mensagens` traduz as demais chaves.
 * Reage aos eventos do controle, então funciona com OnPush e sem zone.js.
 */
@Component({
  selector: 'cw-campo',
  template: `
    <label class="form-label" [for]="para()">
      {{ rotulo() }}
      @if (opcional()) {
        <span class="opcional">opcional</span>
      }
    </label>
    <ng-content />
    @if (erro(); as mensagem) {
      <div class="erro" role="alert">{{ mensagem }}</div>
    } @else if (dica()) {
      <div class="dica">{{ dica() }}</div>
    }
  `,
  host: { '[class.invalido]': '!!erro()' },
  styles: `
    :host {
      display: block;
    }
    .opcional {
      margin-left: 0.25rem;
      font-weight: 400;
      color: var(--cw-ink-faint);
    }
    .dica,
    .erro {
      margin-top: 0.375rem;
      font-size: 0.8125rem;
    }
    .dica {
      color: var(--cw-ink-muted);
    }
    .erro {
      color: var(--cw-danger-ink);
      font-weight: 500;
    }
    :host(.invalido) ::ng-deep .form-control,
    :host(.invalido) ::ng-deep .form-select {
      border-color: var(--cw-danger);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampoComponent {
  readonly rotulo = input.required<string>();
  readonly para = input.required<string>();
  readonly controle = input<AbstractControl>();
  readonly mensagens = input<Readonly<Record<string, string>>>({});
  readonly opcional = input(false);
  readonly dica = input<string>();

  // Qualquer evento do controle (valor, status, touched) re-avalia o erro.
  private readonly versao = toSignal(
    toObservable(this.controle).pipe(
      switchMap((c) => (c ? c.events.pipe(startWith(null)) : [null])),
      map(() => ({})),
    ),
  );

  protected readonly erro = computed(() => {
    this.versao();
    const controle = this.controle();
    const erros = controle?.errors;
    if (!controle || !erros) return null;
    if (typeof erros['servidor'] === 'string') return erros['servidor'];
    if (!controle.touched) return null;
    const chave = Object.keys(erros).find((k) => this.mensagens()[k]);
    return chave ? this.mensagens()[chave] : null;
  });
}

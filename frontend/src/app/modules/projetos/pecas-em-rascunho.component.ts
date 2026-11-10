import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { PecaDoProjetoResumoDto, VersaoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';

/** As peças da composição em rascunho: enquanto houver alguma, a versão não pode ser publicada. Sem repetir a peça. */
export function pecasEmRascunho(versao: VersaoProjetoDto): readonly PecaDoProjetoResumoDto[] {
  const pecas = versao.itens.map((i) => i.peca).filter((p) => p.status === 'RASCUNHO');
  return pecas.filter((p, i) => pecas.findIndex((q) => q.id === p.id) === i);
}

/** Aviso de que a versão não pode ser publicada, com um atalho para cada peça em rascunho (abre a peça no projeto). */
@Component({
  selector: 'cw-pecas-em-rascunho',
  imports: [RouterLink],
  template: `
    <div class="bloqueio" role="alert">
      <p><i class="bi bi-exclamation-triangle" aria-hidden="true"></i> Para publicar a versão {{ numero() }}, publique antes as peças em rascunho:</p>
      <ul>
        @for (peca of pecas(); track peca.id) {
          <li>
            <a [routerLink]="['/projetos', projetoId(), 'pecas', peca.id]">{{ peca.nome }} <i class="bi bi-chevron-right" aria-hidden="true"></i></a>
          </li>
        }
      </ul>
    </div>
  `,
  styles: `
    .bloqueio { margin: 0.5rem 0 1rem; padding: 0.75rem 1rem; border-radius: var(--cw-radius); background: var(--cw-warning-soft); color: var(--cw-warning-ink); }
    p { margin: 0 0 0.375rem; font-weight: 600; }
    ul { display: flex; flex-wrap: wrap; gap: 0.25rem 1rem; margin: 0; padding: 0; list-style: none; }
    a { display: inline-flex; align-items: center; gap: 0.25rem; min-height: var(--cw-alvo); color: inherit; font-weight: 600; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PecasEmRascunhoComponent {
  readonly projetoId = input.required<number>();
  readonly numero = input.required<number>();
  readonly pecas = input.required<readonly PecaDoProjetoResumoDto[]>();
}

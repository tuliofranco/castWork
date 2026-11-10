import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, effect, input, output, viewChild } from '@angular/core';
import type { OrigemArquivo, VersaoDoArquivoDto } from '@data/assistente/dto/ferramenta.dto';

const ROTULOS: Record<OrigemArquivo, string> = {
  Ia: 'Desenhado pela IA',
  Joalheiro: 'Salvo por você',
  Desfazer: 'Ação da IA desfeita',
  Restauracao: 'Restaurado do histórico',
  Conferencia: 'Conferência de peso e materiais',
};

/**
 * Histórico do desenho: cada vez que a IA desenhou, o joalheiro salvou, desfez ou restaurou. Nada é
 * apagado; restaurar grava uma cópia da versão escolhida como a atual. Abre quando recebe a lista.
 */
@Component({
  selector: 'cw-historico-desenho',
  imports: [DatePipe],
  template: `
    <dialog #dialogo class="historico" aria-labelledby="titulo-historico" (close)="fechado.emit()">
      <header>
        <h2 id="titulo-historico">Histórico do desenho</h2>
        <button type="button" class="btn btn-sutil btn-icone" aria-label="Fechar" (click)="dialogo.close()"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
      </header>
      <ol>
        @for (versao of versoes() ?? []; track versao.id; let atual = $first) {
          <li>
            <div class="descricao">
              <strong>{{ rotulo(versao) }}</strong>
              <small>{{ versao.criadoEm | date: 'dd/MM HH:mm' }}@if (atual) { · atual }</small>
            </div>
            <div class="acoes">
              @if (versao.tamanho > 0) {
                <button type="button" class="btn btn-sutil btn-icone" aria-label="Baixar o DXF" (click)="baixar.emit(versao)"><i class="bi bi-download" aria-hidden="true"></i></button>
              }
              @if (!atual) {
                <button type="button" class="btn btn-secundario" [disabled]="ocupado()" (click)="restaurar.emit(versao)">Restaurar</button>
              }
            </div>
          </li>
        } @empty {
          <li class="vazio">O desenho ainda não foi salvo.</li>
        }
      </ol>
    </dialog>
  `,
  styles: `
    .historico {
      width: min(28rem, 100vw - 2rem);
      max-height: min(36rem, 100dvh - 2rem);
      padding: 0;
      border: 1px solid var(--cw-border);
      border-radius: 0.75rem;
      background: var(--cw-surface);
      color: var(--cw-ink);

      &::backdrop {
        background: oklch(0.15 0.01 60 / 0.6);
      }
    }

    header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.75rem 1rem;
      border-bottom: 1px solid var(--cw-border);

      h2 {
        margin: 0;
        font-size: 1.0625rem;
        font-weight: 600;
      }
    }

    ol {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    li {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 0.625rem 1rem;
      border-bottom: 1px solid var(--cw-border);

      &:last-child {
        border-bottom: 0;
      }
    }

    .descricao {
      display: flex;
      flex-direction: column;

      small {
        color: var(--cw-ink-2, inherit);
        opacity: 0.75;
      }
    }

    .acoes {
      display: flex;
      gap: 0.5rem;

      .btn {
        min-height: var(--cw-alvo);
      }
    }

    .vazio {
      justify-content: center;
      opacity: 0.75;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoricoDesenhoComponent {
  readonly versoes = input<readonly VersaoDoArquivoDto[] | null>(null);
  readonly ocupado = input(false);
  readonly restaurar = output<VersaoDoArquivoDto>();
  readonly baixar = output<VersaoDoArquivoDto>();
  readonly fechado = output();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  constructor() {
    effect(() => {
      const dialogo = this.dialogo().nativeElement;
      if (this.versoes() && !dialogo.open) dialogo.showModal();
      else if (!this.versoes() && dialogo.open) dialogo.close();
    });
  }

  protected rotulo(versao: VersaoDoArquivoDto): string {
    return versao.tamanho === 0 ? 'Desenho em branco' : (versao.nome ?? ROTULOS[versao.origem]);
  }
}

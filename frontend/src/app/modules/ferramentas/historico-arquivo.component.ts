import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, input, output, viewChild } from '@angular/core';
import type { OrigemArquivo, VersaoDoArquivoDto } from '@data/assistente/dto/ferramenta.dto';

/** O que muda de uma ferramenta para a outra no histórico: só os textos. */
export type TipoDeArquivo = 'desenho' | 'modelo';

const TEXTOS: Record<TipoDeArquivo, { readonly titulo: string; readonly daIa: string; readonly final: string; readonly baixar: string; readonly vazio: string; readonly emBranco: string }> = {
  desenho: {
    titulo: 'Histórico do desenho',
    daIa: 'Desenhado pela IA',
    final: 'Marcado como final',
    baixar: 'Baixar o DXF',
    vazio: 'O desenho ainda não foi salvo.',
    emBranco: 'Desenho em branco',
  },
  modelo: {
    titulo: 'Histórico do modelo',
    daIa: 'Modelado pela IA',
    final: 'Marcado como final (corpos com o nome da peça)',
    baixar: 'Baixar o projeto',
    vazio: 'O modelo ainda não foi salvo.',
    emBranco: 'Modelo em branco',
  },
};

const ROTULOS: Record<Exclude<OrigemArquivo, 'Ia' | 'NomesDaPeca'>, string> = {
  Joalheiro: 'Salvo por você',
  Desfazer: 'Ação da IA desfeita',
  Restauracao: 'Restaurado do histórico',
  Conferencia: 'Conferência de peso e materiais',
  Biblioteca: 'Cópia da peça da biblioteca',
  Montagem: 'Montado a partir das peças',
};

let historicos = 0;

/**
 * Histórico do arquivo de uma ferramenta (o desenho 2D ou o modelo 3D): cada vez que a IA mexeu nele, o joalheiro
 * salvou, desfez ou restaurou. Nada é
 * apagado; restaurar grava uma cópia da versão escolhida como a atual. Abre quando recebe a lista.
 */
@Component({
  selector: 'cw-historico-arquivo',
  imports: [DatePipe],
  template: `
    <dialog #dialogo class="historico" [attr.aria-labelledby]="idTitulo" (close)="fechado.emit()">
      <header>
        <h2 [id]="idTitulo">{{ titulo() ?? textos().titulo }}</h2>
        <button type="button" class="btn btn-sutil btn-icone" aria-label="Fechar" (click)="dialogo.close()"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
      </header>
      <ol>
        @for (versao of versoes() ?? []; track versao.id; let atual = $first) {
          <li>
            <div class="descricao">
              <strong>{{ rotulo(versao) }}</strong>
              <small>{{ versao.criadoEm | date: 'dd/MM HH:mm' }}@if (atual) { · atual }@if (versao.id === finalId()) { · <strong class="final">final</strong> }</small>
            </div>
            <div class="acoes">
              @if (versao.tamanho > 0) {
                <button type="button" class="btn btn-sutil btn-icone" [attr.aria-label]="textos().baixar" (click)="baixar.emit(versao)"><i class="bi bi-download" aria-hidden="true"></i></button>
              }
              @if (!atual) {
                <button type="button" class="btn btn-secundario" [disabled]="ocupado()" (click)="restaurar.emit(versao)">Restaurar</button>
              }
              @if (podeMarcarFinal() && versao.tamanho > 0 && versao.id !== finalId()) {
                <button type="button" class="btn btn-secundario" [disabled]="ocupado()" (click)="marcarFinal.emit(versao)">Marcar como final</button>
              }
            </div>
          </li>
        } @empty {
          <li class="vazio">{{ textos().vazio }}</li>
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

    .final {
      color: var(--cw-success-ink, inherit);
    }

    .vazio {
      justify-content: center;
      opacity: 0.75;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoricoArquivoComponent {
  /** De qual ferramenta é o arquivo: escolhe os textos. */
  readonly tipo = input.required<TipoDeArquivo>();
  protected readonly textos = computed(() => TEXTOS[this.tipo()]);
  protected readonly idTitulo = `titulo-historico-arquivo-${++historicos}`;

  readonly versoes = input<readonly VersaoDoArquivoDto[] | null>(null);
  readonly ocupado = input(false);
  /** "Elo oval · Modelo 3D" na peça do projeto; sem ele, o título padrão da ferramenta. */
  readonly titulo = input<string | null>(null);
  /** O arquivo final da peça (nulo fora da peça ou sem final). */
  readonly finalId = input<number | null>(null);
  /** Na peça do projeto, com permissão de editar: cada versão com desenho pode virar a final. */
  readonly podeMarcarFinal = input(false);
  readonly marcarFinal = output<VersaoDoArquivoDto>();
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
    if (versao.tamanho === 0) return this.textos().emBranco;
    if (versao.nome) return versao.nome;
    if (versao.origem === 'Ia') return this.textos().daIa;
    if (versao.origem === 'NomesDaPeca') return this.textos().final;
    return ROTULOS[versao.origem];
  }
}

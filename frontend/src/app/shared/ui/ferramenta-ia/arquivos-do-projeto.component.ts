import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, input, output, signal, viewChild } from '@angular/core';

/** Um arquivo do projeto que pode ir anexado à fala (o atual de uma ferramenta ou o PDF do briefing). */
export interface ArquivoDoProjeto {
  readonly origem: string;
  readonly id: number;
  readonly nome: string;
  /** Código da ferramenta que gravou (`desenho-2d`, `modelo-3d`, `briefing`). */
  readonly ferramenta: string;
  /** Número da versão; nulo no que é do projeto (o briefing). */
  readonly versao: number | null;
  readonly criadoEm: string;
}

/** Nome e ícone de cada ferramenta na lista; ferramenta nova sem entrada aparece pelo código. */
const FERRAMENTAS: Record<string, { readonly nome: string; readonly icone: string }> = {
  'desenho-2d': { nome: 'Desenho 2D', icone: 'bi-vector-pen' },
  'modelo-3d': { nome: 'Modelo 3D', icone: 'bi-box' },
  briefing: { nome: 'Briefing', icone: 'bi-file-earmark-pdf' },
};

export function chaveDoArquivo(arquivo: Pick<ArquivoDoProjeto, 'origem' | 'id'>): string {
  return `${arquivo.origem}:${arquivo.id}`;
}

/**
 * "Do projeto": escolhe arquivos do projeto para anexar à fala (até `maximo`, contando os já anexados).
 * Abre quando recebe a lista e devolve a escolha em `escolher`.
 */
@Component({
  selector: 'cw-arquivos-do-projeto',
  imports: [DatePipe],
  template: `
    <dialog #dialogo class="arquivos" aria-labelledby="titulo-arquivos" (close)="fechado.emit()">
      <header>
        <h2 id="titulo-arquivos">Arquivos do projeto</h2>
        <button type="button" class="btn btn-sutil btn-icone" aria-label="Fechar" (click)="dialogo.close()"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
      </header>
      @if (arquivos(); as lista) {
        <ul>
          @for (a of lista; track chave(a)) {
            <li>
              <label [class.indisponivel]="!marcado(a) && cheio()">
                <input type="checkbox" class="form-check-input" [checked]="marcado(a)" [disabled]="!marcado(a) && cheio()" (change)="alternar(a)" />
                <i class="bi" [class]="ferramenta(a).icone" aria-hidden="true"></i>
                <span class="descricao">
                  <strong>{{ a.nome }}</strong>
                  <small>{{ ferramenta(a).nome }}@if (a.versao !== null) { · versão {{ a.versao }}} · {{ a.criadoEm | date: 'dd/MM HH:mm' }}</small>
                </span>
              </label>
            </li>
          } @empty {
            <li class="vazio">Nenhum arquivo no projeto ainda. Salve um desenho, um modelo ou gere o PDF do briefing.</li>
          }
        </ul>
      }
      <footer>
        <small>{{ escolhidos().length }} de {{ livres() }}</small>
        <button type="button" class="btn btn-primary" [disabled]="escolhidos().length === 0" (click)="confirmar()">Anexar</button>
      </footer>
    </dialog>
  `,
  styles: `
    .arquivos {
      width: min(32rem, calc(100vw - 2rem));
      max-height: min(80dvh, 40rem);
      padding: 0;
      border: 1px solid var(--cw-border);
      border-radius: var(--cw-radius-lg);
      background: var(--cw-surface);
      color: var(--cw-ink);

      &::backdrop {
        background: oklch(0.15 0.01 60 / 0.5);
      }
    }

    header,
    footer {
      position: sticky;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      background: var(--cw-surface);
    }

    header {
      top: 0;
      border-bottom: 1px solid var(--cw-border);

      h2 {
        margin: 0;
        font-size: 1.0625rem;
        font-weight: 600;
      }
    }

    footer {
      bottom: 0;
      border-top: 1px solid var(--cw-border);
      color: var(--cw-ink-muted);
    }

    ul {
      margin: 0;
      padding: 0.25rem 0.5rem;
      list-style: none;
    }

    label {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      min-height: var(--cw-alvo);
      padding: 0.5rem;
      border-radius: var(--cw-radius);
      cursor: pointer;

      &:hover {
        background: var(--cw-surface-hover);
      }

      &.indisponivel {
        opacity: 0.5;
        cursor: default;
      }

      .bi {
        color: var(--cw-ink-muted);
        font-size: 1.25rem;
      }
    }

    input {
      flex: none;
      margin: 0;
    }

    .descricao {
      display: flex;
      flex-direction: column;
      min-width: 0;

      strong {
        overflow: hidden;
        font-weight: 600;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      small {
        color: var(--cw-ink-muted);
      }
    }

    .vazio {
      padding: 1rem 0.5rem;
      color: var(--cw-ink-muted);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ArquivosDoProjetoComponent {
  /** Lista para escolher; com valor, o modal fica aberto. */
  readonly arquivos = input<readonly ArquivoDoProjeto[] | null>(null);
  /** Os já anexados à fala: não aparecem marcados de novo e contam no limite. */
  readonly anexados = input<readonly ArquivoDoProjeto[]>([]);
  readonly maximo = input(3);
  readonly escolher = output<readonly ArquivoDoProjeto[]>();
  readonly fechado = output();

  protected readonly escolhidos = signal<readonly ArquivoDoProjeto[]>([]);
  protected readonly livres = computed(() => this.maximo() - this.anexados().length);
  protected readonly cheio = computed(() => this.escolhidos().length >= this.livres());
  protected readonly chave = chaveDoArquivo;

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  constructor() {
    effect(() => {
      const dialogo = this.dialogo().nativeElement;
      if (this.arquivos() && !dialogo.open) {
        this.escolhidos.set([]);
        dialogo.showModal();
      } else if (!this.arquivos() && dialogo.open) {
        dialogo.close();
      }
    });
  }

  protected ferramenta(arquivo: ArquivoDoProjeto): { readonly nome: string; readonly icone: string } {
    return FERRAMENTAS[arquivo.ferramenta] ?? { nome: arquivo.ferramenta, icone: 'bi-file-earmark' };
  }

  protected marcado(arquivo: ArquivoDoProjeto): boolean {
    return this.escolhidos().some((a) => chaveDoArquivo(a) === chaveDoArquivo(arquivo));
  }

  protected alternar(arquivo: ArquivoDoProjeto): void {
    this.escolhidos.update((lista) => (this.marcado(arquivo) ? lista.filter((a) => chaveDoArquivo(a) !== chaveDoArquivo(arquivo)) : [...lista, arquivo]));
  }

  protected confirmar(): void {
    this.escolher.emit(this.escolhidos());
    this.dialogo().nativeElement.close();
  }
}

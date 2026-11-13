import { ChangeDetectionStrategy, Component, computed, effect, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface Etapa {
  readonly id: number;
  readonly nome: string;
  readonly concluida: boolean;
}

const ETAPAS_PADRAO = ['Desenho', 'Modelagem 3D', 'Cera / impressão', 'Fundição', 'Montagem e solda', 'Cravação', 'Acabamento e polimento'];

/**
 * PROTÓTIPO (só frontend): etapas da prototipação de uma versão, marcadas conforme o trabalho avança.
 * O estado fica no navegador (localStorage) até existir o modelo no backend.
 */
function ehEtapa(valor: unknown): valor is Etapa {
  if (typeof valor !== 'object' || valor === null) return false;
  const etapa = valor as Record<string, unknown>;
  return typeof etapa['id'] === 'number' && typeof etapa['nome'] === 'string' && typeof etapa['concluida'] === 'boolean';
}

@Component({
  selector: 'cw-etapas-versao',
  imports: [FormsModule],
  template: `
    <header class="cabecalho">
      <h2 id="titulo-etapas">Etapas</h2>
      <span class="progresso-texto">{{ concluidas() }} de {{ etapas().length }}</span>
    </header>
    <div class="barra" role="progressbar" aria-labelledby="titulo-etapas" [attr.aria-valuenow]="concluidas()" aria-valuemin="0" [attr.aria-valuemax]="etapas().length">
      <span [style.width.%]="percentual()"></span>
    </div>

    <ol class="etapas">
      @for (e of etapas(); track e.id; let i = $index) {
        <li [class.concluida]="e.concluida" [class.atual]="e.id === atual()?.id">
          <button type="button" class="marcador" [disabled]="!editavel()" [attr.aria-pressed]="e.concluida" [attr.aria-label]="(e.concluida ? 'Desmarcar ' : 'Concluir ') + e.nome" (click)="alternar(e.id)">
            @if (e.concluida) {
              <i class="bi bi-check-lg" aria-hidden="true"></i>
            } @else {
              {{ i + 1 }}
            }
          </button>
          <span class="nome">{{ e.nome }}</span>
          @if (e.id === atual()?.id) {
            <span class="agora">agora</span>
          }
          @if (editavel()) {
            <button type="button" class="btn btn-sutil btn-icone remover" [attr.aria-label]="'Remover etapa ' + e.nome" (click)="remover(e.id)">
              <i class="bi bi-x" aria-hidden="true"></i>
            </button>
          }
        </li>
      }
    </ol>

    @if (editavel()) {
      @if (adicionando()) {
        <form class="nova" (ngSubmit)="adicionar()">
          <label class="visually-hidden" for="nova-etapa">Nome da etapa</label>
          <input id="nova-etapa" class="form-control" name="nome" placeholder="Ex.: Prova no cliente" [(ngModel)]="nomeNova" autocomplete="off" />
          <button type="submit" class="btn btn-primary" [disabled]="!nomeNova.trim()">Adicionar</button>
          <button type="button" class="btn btn-sutil" (click)="adicionando.set(false)">Cancelar</button>
        </form>
      } @else {
        <button type="button" class="btn btn-sutil adicionar" (click)="adicionando.set(true)"><i class="bi bi-plus-lg" aria-hidden="true"></i> Etapa</button>
      }
    }
  `,
  styles: `
    :host {
      display: block;
      margin-bottom: 2rem;
    }

    .cabecalho {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      margin-bottom: 0.5rem;

      h2 {
        margin: 0;
        font-size: 1.0625rem;
        font-weight: 600;
      }
    }

    .progresso-texto {
      color: var(--cw-ink-muted);
      font-size: 0.875rem;
      font-variant-numeric: tabular-nums;
    }

    .barra {
      height: 0.25rem;
      margin-bottom: 1rem;
      border-radius: 999px;
      background: var(--cw-surface-2);
      overflow: hidden;

      span {
        display: block;
        height: 100%;
        background: var(--cw-accent);
        transition: width var(--cw-rapido) var(--cw-ease);
      }
    }

    .etapas {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin: 0;
      padding: 0;
      list-style: none;
    }

    li {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.25rem 0.375rem 0.25rem 0.25rem;
      border: 1px solid var(--cw-border);
      border-radius: 999px;
      color: var(--cw-ink-muted);
      font-size: 0.9375rem;

      &.concluida {
        color: var(--cw-ink);

        .marcador {
          border-color: transparent;
          background: var(--cw-accent-soft);
          color: var(--cw-accent-ink);
        }
      }

      &.atual {
        border-color: var(--cw-accent);
        color: var(--cw-ink);
        font-weight: 600;
      }
    }

    .marcador {
      display: grid;
      place-items: center;
      width: 2.25rem;
      height: 2.25rem;
      border: 1px solid var(--cw-border);
      border-radius: 999px;
      background: transparent;
      color: inherit;
      font-size: 0.8125rem;
      font-weight: 600;
      font-variant-numeric: tabular-nums;

      &:disabled {
        cursor: default;
      }
    }

    .nome {
      padding-right: 0.25rem;
    }

    .agora {
      padding: 0.125rem 0.5rem;
      border-radius: 999px;
      background: var(--cw-accent-soft);
      color: var(--cw-accent-ink);
      font-size: 0.75rem;
    }

    .remover {
      width: 1.75rem;
      min-width: 1.75rem;
      height: 1.75rem;
      min-height: 1.75rem;
      padding: 0;
      opacity: 0.6;
    }

    .adicionar {
      margin-top: 0.5rem;
    }

    .nova {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-top: 0.75rem;

      input {
        flex: 1 1 14rem;
        max-width: 22rem;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EtapasVersaoComponent {
  readonly versaoId = input.required<number>();
  readonly editavel = input(true);

  protected readonly etapas = signal<readonly Etapa[]>([]);
  protected readonly adicionando = signal(false);
  protected nomeNova = '';

  protected readonly concluidas = computed(() => this.etapas().filter((e) => e.concluida).length);
  protected readonly percentual = computed(() => (this.etapas().length === 0 ? 0 : (this.concluidas() / this.etapas().length) * 100));
  /** A primeira etapa ainda não concluída é onde o trabalho está agora. */
  protected readonly atual = computed(() => this.etapas().find((e) => !e.concluida));

  private carregado = false;

  constructor() {
    effect(() => {
      const chave = this.chave();
      let salvas: Etapa[] | null = null;
      try {
        const lido: unknown = JSON.parse(localStorage.getItem(chave) ?? 'null');
        // O localStorage é do navegador, não do servidor: só vale se for mesmo uma lista de etapas.
        salvas = Array.isArray(lido) && lido.every(ehEtapa) ? lido : null;
      } catch {
        salvas = null;
      }
      this.etapas.set(salvas ?? ETAPAS_PADRAO.map((nome, i) => ({ id: i + 1, nome, concluida: false })));
      this.carregado = true;
    });
    effect(() => {
      const etapas = this.etapas();
      if (!this.carregado) return;
      try {
        localStorage.setItem(this.chave(), JSON.stringify(etapas));
      } catch {
        // Sem armazenamento (aba privada): o protótipo funciona só nesta visita.
      }
    });
  }

  protected alternar(id: number): void {
    this.etapas.update((lista) => lista.map((e) => (e.id === id ? { ...e, concluida: !e.concluida } : e)));
  }

  protected remover(id: number): void {
    this.etapas.update((lista) => lista.filter((e) => e.id !== id));
  }

  protected adicionar(): void {
    const nome = this.nomeNova.trim();
    if (!nome) return;
    this.etapas.update((lista) => [...lista, { id: Math.max(0, ...lista.map((e) => e.id)) + 1, nome, concluida: false }]);
    this.nomeNova = '';
    this.adicionando.set(false);
  }

  private chave(): string {
    return `castwork.prototipo.etapas.${this.versaoId()}`;
  }
}

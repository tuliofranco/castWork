import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

export type ModeloIa = 'haiku' | 'sonnet' | 'opus';
export type EsforcoIa = 'low' | 'medium' | 'high';

export interface EscolhaDaIa {
  readonly modelo: ModeloIa;
  readonly esforco: EsforcoIa;
}

/** Modelos que o joalheiro pode escolher, do mais barato ao mais caro. */
export const MODELOS: readonly { readonly valor: ModeloIa; readonly nome: string; readonly dica: string }[] = [
  { valor: 'haiku', nome: 'Haiku', dica: 'Mais rápido e econômico' },
  { valor: 'sonnet', nome: 'Sonnet', dica: 'Equilibrado' },
  { valor: 'opus', nome: 'Opus', dica: 'Mais capaz e mais caro' },
];

export const ESFORCOS: readonly { readonly valor: EsforcoIa; readonly nome: string }[] = [
  { valor: 'low', nome: 'Esforço baixo' },
  { valor: 'medium', nome: 'Esforço médio' },
  { valor: 'high', nome: 'Esforço alto' },
];

export const ESCOLHA_PADRAO: EscolhaDaIa = { modelo: 'sonnet', esforco: 'medium' };

/** Padrão de cada ferramenta até o joalheiro escolher outro: o briefing é só conversa, o Haiku dá conta. */
const PADRAO_POR_FERRAMENTA: Readonly<Record<string, EscolhaDaIa>> = {
  briefing: { modelo: 'haiku', esforco: 'medium' },
};

export function escolhaPadrao(ferramenta: string): EscolhaDaIa {
  return PADRAO_POR_FERRAMENTA[ferramenta] ?? ESCOLHA_PADRAO;
}

/**
 * Modelo e esforço da IA, discretos no rodapé do chat: parecem texto, abrem o seletor nativo
 * (no tablet, a roda de opções do sistema).
 */
@Component({
  selector: 'cw-escolha-ia',
  template: `
    <label class="opcao" [title]="dicaDoModelo()">
      <i class="bi bi-stars" aria-hidden="true"></i>
      <span class="visually-hidden">Modelo da IA</span>
      <select [disabled]="desabilitado()" [value]="escolha().modelo" (change)="mudar({ modelo: $any($event.target).value })">
        @for (m of modelos; track m.valor) {
          <option [value]="m.valor" [selected]="m.valor === escolha().modelo">{{ m.nome }}</option>
        }
      </select>
      <i class="bi bi-chevron-down seta" aria-hidden="true"></i>
    </label>
    <label class="opcao" title="Quanto a IA pensa antes de agir: menos esforço gasta menos">
      <span class="visually-hidden">Esforço da IA</span>
      <select [disabled]="desabilitado()" [value]="escolha().esforco" (change)="mudar({ esforco: $any($event.target).value })">
        @for (e of esforcos; track e.valor) {
          <option [value]="e.valor" [selected]="e.valor === escolha().esforco">{{ e.nome }}</option>
        }
      </select>
      <i class="bi bi-chevron-down seta" aria-hidden="true"></i>
    </label>
  `,
  styles: `
    // Linha própria embaixo da caixa de texto (o compositor é flex com quebra).
    :host {
      display: flex;
      flex-basis: 100%;
      flex-wrap: wrap;
      gap: 0.25rem;
      margin: -0.25rem 0 -0.25rem -0.25rem;
    }

    .opcao {
      position: relative;
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      min-height: 2rem;
      padding: 0 0.5rem;
      border-radius: 999px;
      color: var(--cw-ink-muted);
      font-size: 0.8125rem;
      cursor: pointer;

      &:hover,
      &:focus-within {
        background: var(--cw-surface-hover);
        color: var(--cw-ink);
      }

      &:has(select:focus-visible) {
        box-shadow: var(--cw-focus);
      }

      &:has(select:disabled) {
        opacity: 0.5;
        cursor: default;
      }
    }

    select {
      appearance: none;
      padding: 0 1rem 0 0;
      margin-right: -1rem;
      border: 0;
      background: transparent;
      color: inherit;
      font: inherit;
      cursor: inherit;

      &:focus {
        outline: none;
      }
    }

    .seta {
      font-size: 0.625rem;
      pointer-events: none;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EscolhaIaComponent {
  readonly escolha = model<EscolhaDaIa>(ESCOLHA_PADRAO);
  readonly desabilitado = input(false);

  protected readonly modelos = MODELOS;
  protected readonly esforcos = ESFORCOS;

  protected dicaDoModelo(): string {
    return MODELOS.find((m) => m.valor === this.escolha().modelo)?.dica ?? '';
  }

  protected mudar(mudanca: Partial<EscolhaDaIa>): void {
    this.escolha.update((atual) => ({ ...atual, ...mudanca }));
  }
}

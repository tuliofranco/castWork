import { computed, signal } from '@angular/core';

/**
 * Navegação para frente e para trás numa connection paginada por cursor.
 * Guarda o cursor de início de cada página visitada: "anterior" é voltar para o cursor guardado.
 */
export class PaginacaoCursor {
  private readonly historico = signal<readonly (string | null)[]>([]);
  private readonly _atual = signal<string | null>(null);

  readonly apos = this._atual.asReadonly();
  readonly numero = computed(() => this.historico().length + 1);
  readonly temAnterior = computed(() => this.historico().length > 0);

  proxima(endCursor: string | null): void {
    // O mesmo cursor de novo (duplo clique em "próxima") empilharia a página atual no histórico.
    if (!endCursor || endCursor === this._atual()) return;
    this.historico.update((h) => [...h, this._atual()]);
    this._atual.set(endCursor);
  }

  anterior(): void {
    const h = this.historico();
    if (h.length === 0) return;
    this._atual.set(h[h.length - 1] ?? null);
    this.historico.set(h.slice(0, -1));
  }

  reiniciar(): void {
    this.historico.set([]);
    this._atual.set(null);
  }
}

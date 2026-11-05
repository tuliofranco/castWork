import { Injectable, signal } from '@angular/core';

export type TipoToast = 'sucesso' | 'erro' | 'aviso' | 'info';

export interface Toast {
  readonly id: number;
  readonly tipo: TipoToast;
  readonly mensagem: string;
}

const DURACAO_MS = 5000;

/** Fila de toasts em signal; a UI (ToastContainerComponent) só lê `toasts`. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private proximoId = 1;
  private readonly _toasts = signal<readonly Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  sucesso(mensagem: string): void {
    this.exibir('sucesso', mensagem);
  }

  erro(mensagem: string): void {
    this.exibir('erro', mensagem);
  }

  aviso(mensagem: string): void {
    this.exibir('aviso', mensagem);
  }

  info(mensagem: string): void {
    this.exibir('info', mensagem);
  }

  remover(id: number): void {
    this._toasts.update((lista) => lista.filter((toast) => toast.id !== id));
  }

  private exibir(tipo: TipoToast, mensagem: string): void {
    // Evita empilhar a mesma mensagem quando várias requisições falham juntas (ex.: servidor fora).
    if (this._toasts().some((toast) => toast.tipo === tipo && toast.mensagem === mensagem)) return;
    const id = this.proximoId++;
    this._toasts.update((lista) => [...lista, { id, tipo, mensagem }]);
    setTimeout(() => this.remover(id), DURACAO_MS);
  }
}

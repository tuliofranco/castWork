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

  private readonly temporizadores = new Map<number, ReturnType<typeof setTimeout>>();

  remover(id: number): void {
    clearTimeout(this.temporizadores.get(id));
    this.temporizadores.delete(id);
    this._toasts.update((lista) => lista.filter((toast) => toast.id !== id));
  }

  private exibir(tipo: TipoToast, mensagem: string): void {
    // Evita empilhar a mesma mensagem quando várias requisições falham juntas (ex.: servidor fora).
    // A repetição renova o tempo do aviso que já está na tela, em vez de deixá-lo sumir logo depois.
    const repetido = this._toasts().find((toast) => toast.tipo === tipo && toast.mensagem === mensagem);
    const id = repetido?.id ?? this.proximoId++;
    if (!repetido) this._toasts.update((lista) => [...lista, { id, tipo, mensagem }]);
    clearTimeout(this.temporizadores.get(id));
    // Erro fica mais tempo: é o que a pessoa precisa conseguir ler.
    this.temporizadores.set(id, setTimeout(() => this.remover(id), tipo === 'erro' ? DURACAO_MS * 2 : DURACAO_MS));
  }
}

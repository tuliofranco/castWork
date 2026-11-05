import { Injectable } from '@angular/core';

/**
 * Acesso tipado ao localStorage. Os acessos ficam em try/catch porque o navegador pode
 * bloquear o storage (modo privado, cota cheia, cookies desativados).
 */
@Injectable({ providedIn: 'root' })
export class LocalStorageService {
  obter(chave: string): string | null {
    try {
      return localStorage.getItem(chave);
    } catch {
      return null;
    }
  }

  definir(chave: string, valor: string): void {
    try {
      localStorage.setItem(chave, valor);
    } catch {
      // Sem storage a sessão vive só em memória até o recarregamento.
    }
  }

  remover(chave: string): void {
    try {
      localStorage.removeItem(chave);
    } catch {
      // Nada a remover se o storage está indisponível.
    }
  }
}

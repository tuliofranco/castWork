import { Injectable, inject } from '@angular/core';

import { SessaoService } from './sessao.service';

@Injectable({ providedIn: 'root' })
export class PermissaoService {
  private readonly sessao = inject(SessaoService);

  /**
   * true se o usuário tem AO MENOS UMA das permissões (lista vazia = liberado).
   * Lê um signal: chamado dentro de computed/template, reage à troca de usuário.
   */
  temAlguma(permissoes: readonly string[]): boolean {
    if (permissoes.length === 0) return true;
    const doUsuario = this.sessao.permissoes();
    return permissoes.some((permissao) => doUsuario.has(permissao));
  }

  temTodas(permissoes: readonly string[]): boolean {
    const doUsuario = this.sessao.permissoes();
    return permissoes.every((permissao) => doUsuario.has(permissao));
  }
}

import { Injectable, computed, inject } from '@angular/core';

import type { TokenPayload } from '../models/token-payload.model';
import { AuthService } from './auth.service';
import { JwtService } from './jwt.service';

export interface UsuarioSessao {
  readonly id: string;
  readonly nome: string;
  readonly email: string;
}

export interface AtelieSessao {
  readonly nome: string;
}

/**
 * Visão somente-leitura do token decodificado, em signals derivados (computed).
 * Quando o AuthService troca o token, tudo aqui recalcula sozinho: não há estado duplicado.
 */
@Injectable({ providedIn: 'root' })
export class SessaoService {
  private readonly auth = inject(AuthService);
  private readonly jwt = inject(JwtService);

  private readonly payload = computed(() => this.jwt.decodificar<TokenPayload>(this.auth.token()));

  readonly usuario = computed<UsuarioSessao | null>(() => {
    const p = this.payload();
    return p ? { id: p.sub, nome: p.nome, email: p.email } : null;
  });

  readonly atelie = computed<AtelieSessao | null>(() => {
    const p = this.payload();
    return p ? { nome: p.atelieNome } : null;
  });

  /** Set para busca O(1); normaliza o caso em que o emissor manda string única. */
  readonly permissoes = computed<ReadonlySet<string>>(() => {
    const bruto = this.payload()?.permissoes ?? [];
    return new Set(typeof bruto === 'string' ? [bruto] : bruto);
  });
}

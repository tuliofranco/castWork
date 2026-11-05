import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import type { PostLoginDto } from '@data/auth/login/dto/post-login.dto';
import { LoginService } from '@data/auth/login/login.service';
import { LocalStorageService } from '@shared/services/local-storage.service';
import { type Observable, map, tap } from 'rxjs';

import { JwtService } from './jwt.service';

export const CHAVE_ACCESS_TOKEN = 'castwork.accessToken';
export const CHAVE_REFRESH_TOKEN = 'castwork.refreshToken';

/** Margem para considerar o token vencido antes da hora (relógios desencontrados, latência). */
const MARGEM_EXPIRACAO_SEGUNDOS = 10;

/**
 * Dono do token. Guarda em signal (para a UI reagir) e no localStorage (para sobreviver ao F5).
 * Não decodifica claims para a UI: isso é papel do SessaoService.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly loginService = inject(LoginService);
  private readonly storage = inject(LocalStorageService);
  private readonly jwt = inject(JwtService);
  private readonly router = inject(Router);

  private readonly _token = signal(this.storage.obter(CHAVE_ACCESS_TOKEN));
  readonly token = this._token.asReadonly();

  login(dto: PostLoginDto): Observable<void> {
    return this.loginService.postLogin(dto).pipe(
      tap((resposta) => {
        this.storage.definir(CHAVE_ACCESS_TOKEN, resposta.accessToken);
        this.storage.definir(CHAVE_REFRESH_TOKEN, resposta.refreshToken);
        this._token.set(resposta.accessToken);
      }),
      map(() => undefined),
    );
  }

  /** Existe token E ele ainda não venceu. Um token vencido no storage não conta como logado. */
  isLoggedIn(): boolean {
    return !this.jwt.expirado(this._token(), MARGEM_EXPIRACAO_SEGUNDOS);
  }

  logout(returnTo?: string): void {
    this.limparSessao();
    void this.router.navigate(['/login'], { queryParams: returnTo ? { returnTo } : {} });
  }

  limparSessao(): void {
    this.storage.remover(CHAVE_ACCESS_TOKEN);
    this.storage.remover(CHAVE_REFRESH_TOKEN);
    this._token.set(null);
  }
}

import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import type { PostLoginDto } from '@data/auth/login/dto/post-login.dto';
import { LoginService } from '@data/auth/login/login.service';
import { LogoutService } from '@data/auth/logout/logout.service';
import { RefreshTokenService } from '@data/auth/refresh-token/refresh-token.service';
import { LocalStorageService } from '@shared/services/local-storage.service';
import { type Observable, finalize, map, shareReplay, tap, throwError } from 'rxjs';

import { JwtService } from './jwt.service';

export const CHAVE_ACCESS_TOKEN = 'castwork.accessToken';
export const CHAVE_REFRESH_TOKEN = 'castwork.refreshToken';

/** Margem para considerar o token vencido antes da hora (relógios desencontrados, latência). */
const MARGEM_EXPIRACAO_SEGUNDOS = 10;

/**
 * O refresh falhou de vez (o refresh token não vale mais) ou só agora (rede, 429, 5xx)? Só a falha definitiva encerra a
 * sessão: deslogar numa queda de rede jogaria fora um refresh token válido e a tela em que a pessoa estava.
 */
export function refreshFalhouDeVez(erro: unknown): boolean {
  return !(erro instanceof HttpErrorResponse) || [400, 401, 403].includes(erro.status);
}

/**
 * Dono do token. Guarda em signal (para a UI reagir) e no localStorage (para sobreviver ao F5).
 * Não decodifica claims para a UI: isso é papel do SessaoService.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly loginService = inject(LoginService);
  private readonly refreshTokenService = inject(RefreshTokenService);
  private readonly logoutService = inject(LogoutService);
  private readonly storage = inject(LocalStorageService);
  private readonly jwt = inject(JwtService);
  private readonly router = inject(Router);

  private readonly _token = signal(this.storage.obter(CHAVE_ACCESS_TOKEN));
  readonly token = this._token.asReadonly();

  /** Refresh em andamento, compartilhado por todas as requisições que receberem 401 ao mesmo tempo. */
  private refreshEmAndamento$: Observable<string> | null = null;
  /** Muda a cada sessão encerrada: um refresh que termina depois do "Sair" não regrava os tokens. */
  private epoca = 0;

  login(dto: PostLoginDto): Observable<void> {
    return this.loginService.postLogin(dto).pipe(
      tap((resposta) => this.gravarTokens(resposta.accessToken, resposta.refreshToken)),
      map(() => undefined),
    );
  }

  /**
   * Renova o access token e emite o novo valor.
   *
   * Condição de corrida: a tela abre e dispara 3 requisições; o token venceu e as 3 voltam 401
   * quase juntas. Sem cuidado, sairiam 3 refreshes — e, com rotação de refresh token, o 2º e o 3º
   * usariam um refresh já invalidado pelo 1º, derrubando a sessão.
   *
   * Solução: a 1ª chamada cria o Observable e o guarda; as seguintes reutilizam o mesmo.
   * `shareReplay(1)` faz uma única requisição e entrega o resultado a todos, inclusive a quem
   * se inscrever depois que ela terminou. `finalize` limpa a referência para o PRÓXIMO ciclo.
   */
  renovarToken(): Observable<string> {
    const refreshToken = this.storage.obter(CHAVE_REFRESH_TOKEN);
    if (!refreshToken) return throwError(() => new Error('Sem refresh token'));

    const epoca = this.epoca;
    this.refreshEmAndamento$ ??= this.refreshTokenService.postRefreshToken({ refreshToken }).pipe(
      tap((resposta) => {
        if (epoca !== this.epoca) throw new Error('Sessão encerrada');
        this.gravarTokens(resposta.accessToken, resposta.refreshToken);
      }),
      map((resposta) => resposta.accessToken),
      finalize(() => (this.refreshEmAndamento$ = null)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.refreshEmAndamento$;
  }

  /** Existe token E ele ainda não venceu. Um token vencido no storage não conta como logado. */
  isLoggedIn(): boolean {
    return !this.jwt.expirado(this._token(), MARGEM_EXPIRACAO_SEGUNDOS);
  }

  /** O access token pode ter vencido, mas ainda dá para renovar a sessão. */
  temRefreshToken(): boolean {
    return !!this.storage.obter(CHAVE_REFRESH_TOKEN);
  }

  /**
   * Encerra a sessão no servidor (revoga o refresh token) e localmente. Não espera a resposta:
   * mesmo offline o usuário sai, e o refresh token ainda expira sozinho no servidor.
   */
  logout(returnTo?: string): void {
    const refreshToken = this.storage.obter(CHAVE_REFRESH_TOKEN);
    // Já encerrada (várias requisições falharam juntas): não navega de novo, para não trocar o returnTo do primeiro.
    if (!refreshToken && !this._token()) return;
    if (refreshToken) this.logoutService.postLogout({ refreshToken }).subscribe({ error: () => undefined });
    this.limparSessao();
    void this.router.navigate(['/login'], { queryParams: returnTo ? { returnTo } : {} });
  }

  private gravarTokens(accessToken: string, refreshToken: string): void {
    this.storage.definir(CHAVE_ACCESS_TOKEN, accessToken);
    this.storage.definir(CHAVE_REFRESH_TOKEN, refreshToken);
    this._token.set(accessToken);
  }

  limparSessao(): void {
    this.epoca++;
    this.refreshEmAndamento$ = null;
    this.storage.remover(CHAVE_ACCESS_TOKEN);
    this.storage.remover(CHAVE_REFRESH_TOKEN);
    this._token.set(null);
  }
}

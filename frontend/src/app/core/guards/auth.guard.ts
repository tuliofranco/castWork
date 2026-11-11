import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';
import { AuthService, refreshFalhouDeVez } from '@core/services/auth.service';
import { catchError, map, of } from 'rxjs';

/**
 * Sem sessão válida → /login?returnTo=<url pedida>.
 * Devolver UrlTree (em vez de chamar navigate) deixa o router cancelar e redirecionar
 * numa única navegação, sem histórico intermediário.
 *
 * Access token vencido com refresh token guardado ainda é sessão: renova antes de decidir. Só a recusa do refresh
 * (400/401/403) apaga a sessão; numa falha passageira (rede, 5xx) vai ao login sem jogar o refresh token fora.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.isLoggedIn()) return true;

  const login = inject(Router).createUrlTree(['/login'], { queryParams: { returnTo: state.url } });
  if (!auth.temRefreshToken()) {
    auth.limparSessao(); // token vencido no storage não deve sobrar
    return login;
  }
  return auth.renovarToken().pipe(
    map(() => true as const),
    catchError((erro: unknown) => {
      if (refreshFalhouDeVez(erro)) auth.limparSessao();
      return of(login);
    }),
  );
};

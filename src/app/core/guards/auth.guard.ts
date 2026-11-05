import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

/**
 * Sem sessão válida → /login?returnTo=<url pedida>.
 * Devolver UrlTree (em vez de chamar navigate) deixa o router cancelar e redirecionar
 * numa única navegação, sem histórico intermediário.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.isLoggedIn()) return true;

  auth.limparSessao(); // token vencido no storage não deve sobrar
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnTo: state.url } });
};

import { HttpErrorResponse, type HttpHandlerFn, type HttpInterceptorFn, type HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, refreshFalhouDeVez } from '@core/services/auth.service';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { BYPASS_AUTH } from '@shared/injection-tokens/http-context.tokens';
import { catchError, switchMap, throwError } from 'rxjs';

function comToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

/**
 * 2. Anexa o JWT e, no 401, tenta UM refresh e repete a requisição.
 *
 * Fluxo:
 *   requisição → 401 → renovarToken() (compartilhado) → repete com o token novo
 *                                      ├─ refresh recusado (400/401/403) → limpa sessão, vai ao login, propaga o 401
 *                                      └─ rede, 429 ou 5xx → mantém a sessão e propaga o erro do refresh
 *
 * Só envia o token para a nossa API: mandar o Bearer para uma URL de terceiro vazaria a credencial.
 */
export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const ambiente = inject(AMBIENTE);
  const token = auth.token();
  const ehNossaApi = req.url.startsWith(ambiente.apiUrl) || req.url.startsWith(ambiente.graphqlUrl);

  if (req.context.get(BYPASS_AUTH) || !ehNossaApi || !token) return next(req);

  // inject() só funciona na parte SÍNCRONA do interceptor. O catchError roda depois, fora do
  // contexto de injeção — por isso o Router é resolvido aqui e passado adiante.
  const router = inject(Router);
  return next(comToken(req, token)).pipe(
    catchError((erro: unknown) =>
      erro instanceof HttpErrorResponse && erro.status === 401
        ? renovarERepetir(req, next, erro, auth, router)
        : throwError(() => erro),
    ),
  );
};

function renovarERepetir(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  erroOriginal: HttpErrorResponse,
  auth: AuthService,
  router: Router,
) {
  return auth.renovarToken().pipe(
    catchError((erroDoRefresh: unknown) => {
      // Falha passageira: a sessão continua valendo e quem chamou recebe o erro de rede/servidor, não um 401.
      if (!refreshFalhouDeVez(erroDoRefresh)) return throwError(() => erroDoRefresh);
      auth.logout(router.url.startsWith('/login') ? undefined : router.url);
      return throwError(() => erroOriginal);
    }),
    // A repetição usa `next` direto: se der 401 de novo, não entra em outro ciclo de refresh.
    switchMap((novoToken) => next(comToken(req, novoToken))),
  );
}

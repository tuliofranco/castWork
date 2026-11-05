import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BYPASS_LOADING } from '@shared/injection-tokens/http-context.tokens';
import { LoadingService } from '@shared/services/loading.service';
import { finalize } from 'rxjs';

/**
 * 6. Watcher de requisições para o loading global.
 * `finalize` cobre sucesso, erro E cancelamento (ex.: switchMap descartando a requisição anterior);
 * com tap/catchError o contador vazaria no cancelamento.
 */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.context.get(BYPASS_LOADING)) return next(req);

  const loading = inject(LoadingService);
  loading.iniciar();
  return next(req).pipe(finalize(() => loading.encerrar()));
};

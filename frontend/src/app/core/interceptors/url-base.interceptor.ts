import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { BYPASS_URL_BASE } from '@shared/injection-tokens/http-context.tokens';

const URL_ABSOLUTA = /^[a-z][a-z\d+.-]*:\/\//i;

/** Junta base e caminho com exatamente uma barra entre eles (sem regex: evita backtracking). */
function juntarUrl(base: string, caminho: string): string {
  let fim = base.length;
  while (fim > 0 && base[fim - 1] === '/') fim--;
  let inicio = 0;
  while (inicio < caminho.length && caminho[inicio] === '/') inicio++;
  return `${base.slice(0, fim)}/${caminho.slice(inicio)}`;
}

/**
 * 1. Prefixa `apiUrl` nas URLs relativas: os data services escrevem só `pecas/123`.
 * O endpoint GraphQL é uma base própria: com `graphqlUrl` relativo (`/graphql`, atrás do proxy),
 * ele viraria `/api/graphql` e daria 404.
 */
export const urlBaseInterceptor: HttpInterceptorFn = (req, next) => {
  const ambiente = inject(AMBIENTE);
  const jaTemBase = URL_ABSOLUTA.test(req.url) || req.url.startsWith(ambiente.graphqlUrl);
  if (req.context.get(BYPASS_URL_BASE) || jaTemBase) return next(req);

  return next(req.clone({ url: juntarUrl(ambiente.apiUrl, req.url) }));
};

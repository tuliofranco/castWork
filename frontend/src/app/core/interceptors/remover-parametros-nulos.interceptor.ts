import type { HttpInterceptorFn } from '@angular/common/http';

/** Como o HttpParams transforma tudo em texto, null/undefined chegam aqui como 'null'/'undefined'. */
const VAZIOS = new Set(['null', 'undefined', '']);

/**
 * 3. Remove da query string os filtros não preenchidos.
 * Sem isto, `{ status: undefined }` viraria `?status=undefined` e a API filtraria pela palavra "undefined".
 */
export const removerParametrosNulosInterceptor: HttpInterceptorFn = (req, next) => {
  const chaves = req.params.keys();
  if (chaves.length === 0) return next(req);

  // HttpParams é imutável: cada delete/append devolve uma nova instância (preservando o encoder).
  let params = req.params;
  for (const chave of chaves) {
    const valores = req.params.getAll(chave) ?? [];
    const validos = valores.filter((valor) => !VAZIOS.has(valor));
    if (validos.length === valores.length) continue;
    params = params.delete(chave);
    for (const valor of validos) params = params.append(chave, valor);
  }
  return next(params === req.params ? req : req.clone({ params }));
};

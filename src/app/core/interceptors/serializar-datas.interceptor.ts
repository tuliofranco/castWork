import type { HttpInterceptorFn } from '@angular/common/http';

/**
 * Formato de `Date.prototype.toString()`, que é o que o HttpParams produz ao receber um Date.
 * Ex.: "Thu Nov 05 2026 00:00:00 GMT-0300 (Horário Padrão de Brasília)"
 */
const DATE_TO_STRING = /^[A-Z][a-z]{2} [A-Z][a-z]{2} \d{2} \d{4} \d{2}:\d{2}:\d{2} GMT[+-]\d{4}/;

function doisDigitos(numero: number): string {
  return String(numero).padStart(2, '0');
}

/**
 * Data "pura" (meia-noite local) vira `yyyy-MM-dd`: é um filtro de dia, e mandar em UTC
 * empurraria 05/11 00:00 (-03:00) para 05/11 03:00Z — ou, em fuso positivo, para o dia anterior.
 * Qualquer outro horário vira ISO 8601 em UTC.
 */
export function serializarData(data: Date): string {
  const meiaNoite = data.getHours() === 0 && data.getMinutes() === 0 && data.getSeconds() === 0;
  if (!meiaNoite) return data.toISOString();
  return `${data.getFullYear()}-${doisDigitos(data.getMonth() + 1)}-${doisDigitos(data.getDate())}`;
}

/** 4. Converte datas dos query params para um formato que a API entende. */
export const serializarDatasInterceptor: HttpInterceptorFn = (req, next) => {
  let params = req.params;
  for (const chave of req.params.keys()) {
    const valores = req.params.getAll(chave) ?? [];
    if (!valores.some((valor) => DATE_TO_STRING.test(valor))) continue;
    params = params.delete(chave);
    for (const valor of valores) {
      params = params.append(chave, DATE_TO_STRING.test(valor) ? serializarData(new Date(valor)) : valor);
    }
  }
  return next(params === req.params ? req : req.clone({ params }));
};

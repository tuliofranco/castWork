import { HttpParams } from '@angular/common/http';

export type ValorParametro = string | number | boolean | Date | null | undefined;
export type ParametrosConsulta = Readonly<Record<string, ValorParametro | readonly ValorParametro[]>>;

/**
 * Aceita a query tipada da tela (com Date, null e undefined) e devolve HttpParams.
 * Não limpa nem formata nada de propósito: isso é papel dos interceptors
 * `removerParametrosNulos` e `serializarDatas`, que valem para todas as requisições.
 */
export function criarHttpParams(parametros: ParametrosConsulta): HttpParams {
  // O tipo de fromObject não aceita Date/null, mas em runtime ele só faz `${valor}`. O cast é intencional.
  return new HttpParams({ fromObject: parametros as Record<string, string> });
}

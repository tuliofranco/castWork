import { type OperatorFunction, map } from 'rxjs';

interface ResultadoGql {
  readonly data?: unknown;
  readonly error?: unknown;
}

export class RespostaGqlSemDadosError extends Error {
  constructor(readonly causa: unknown) {
    super('A resposta GraphQL não trouxe dados.');
    this.name = 'RespostaGqlSemDadosError';
  }
}

/**
 * Trata a resposta com `data` e `errors` ao mesmo tempo (errorPolicy: 'all'):
 * - veio `data` (mesmo com erros): emite `data`. Os erros já viraram toast no ErrorLink;
 *   a tela mostra o que conseguiu carregar.
 * - não veio `data`: lança, para o `error` do subscribe tratar como falha.
 */
export function dadosGql<R extends ResultadoGql>(): OperatorFunction<R, NonNullable<R['data']>> {
  return map((resultado) => {
    if (resultado.data !== undefined && resultado.data !== null) return resultado.data as NonNullable<R['data']>;
    throw new RespostaGqlSemDadosError(resultado.error);
  });
}

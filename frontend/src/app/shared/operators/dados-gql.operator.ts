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

/** Algum campo raiz pedido veio nulo (`{ projeto: null }`): com erro na resposta, foi o resolver dele que falhou. */
function campoRaizNulo(data: unknown): boolean {
  return typeof data === 'object' && data !== null && Object.values(data).some((valor) => valor === null);
}

/**
 * Trata a resposta com `data` e `errors` ao mesmo tempo (errorPolicy: 'all'):
 * - veio `data` completo (mesmo com erros em campos internos): emite `data`. Os erros já viraram toast no ErrorLink;
 *   a tela mostra o que conseguiu carregar.
 * - não veio `data`, ou veio erro junto de um campo raiz nulo: lança, para a tela mostrar o estado de erro. Sem isso,
 *   a falha do resolver chegava como "não encontrado" ou lista vazia.
 */
export function dadosGql<R extends ResultadoGql>(): OperatorFunction<R, NonNullable<R['data']>> {
  return map((resultado) => {
    const semDados = resultado.data === undefined || resultado.data === null;
    const raizFalhou = resultado.error !== undefined && resultado.error !== null && campoRaizNulo(resultado.data);
    if (!semDados && !raizFalhou) return resultado.data as NonNullable<R['data']>;
    throw new RespostaGqlSemDadosError(resultado.error);
  });
}

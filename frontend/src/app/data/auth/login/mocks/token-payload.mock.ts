import { faker } from '@faker-js/faker/locale/pt_BR';
import type { TokenPayload } from '@core/models/token-payload.model';

import { codificarTokenMock } from './codificar-token.mock';

/** Payload aleatório para testes; sobrescreva só o que importa para o cenário. */
export function criarTokenPayloadMock(sobrescrever: Partial<TokenPayload> = {}): TokenPayload {
  return {
    sub: faker.string.uuid(),
    nome: faker.person.fullName(),
    email: faker.internet.email(),
    atelieNome: `Ateliê ${faker.company.name()}`,
    permissoes: ['pecas.ler'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    ...sobrescrever,
  };
}

/** JWT codificado pronto para gravar no storage nos testes. */
export function criarTokenMock(sobrescrever: Partial<TokenPayload> = {}): string {
  return codificarTokenMock(criarTokenPayloadMock(sobrescrever));
}

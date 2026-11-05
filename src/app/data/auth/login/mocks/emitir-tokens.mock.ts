import { codificarTokenMock } from './codificar-token.mock';
import { USUARIOS_DEMO } from './usuarios-demo.mock';

const OITO_HORAS_EM_SEGUNDOS = 8 * 60 * 60;
const PREFIXO_REFRESH = 'refresh-mock:';

export interface TokensMock {
  readonly accessToken: string;
  readonly refreshToken: string;
}

/** Emite o par de tokens de um usuário demo; null se o e-mail não existe. */
export function emitirTokensMock(email: string): TokensMock | null {
  const usuario = USUARIOS_DEMO[email.toLowerCase()];
  if (!usuario) return null;
  const exp = Math.floor(Date.now() / 1000) + OITO_HORAS_EM_SEGUNDOS;
  return {
    accessToken: codificarTokenMock({ ...usuario, exp }),
    // No mock o refresh token carrega o e-mail; um real é opaco e só o servidor sabe a quem pertence.
    refreshToken: `${PREFIXO_REFRESH}${usuario.email}`,
  };
}

export function emailDoRefreshMock(refreshToken: string): string | null {
  return refreshToken.startsWith(PREFIXO_REFRESH) ? refreshToken.slice(PREFIXO_REFRESH.length) : null;
}

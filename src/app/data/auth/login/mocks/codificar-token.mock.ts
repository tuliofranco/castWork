import type { TokenPayload } from '@core/models/token-payload.model';

function base64Url(texto: string): string {
  // TextEncoder garante UTF-8 (nomes com acento); btoa só aceita Latin-1.
  const bytes = new TextEncoder().encode(texto);
  const binario = Array.from(bytes, (byte) => String.fromCodePoint(byte)).join('');
  return btoa(binario).replaceAll('+', '-').replaceAll('/', '_').replaceAll('=', '');
}

/**
 * Monta um JWT NÃO assinado (assinatura falsa). Serve só para o front decodificar:
 * o front nunca valida assinatura; quem valida é sempre o servidor.
 */
export function codificarTokenMock(payload: TokenPayload): string {
  const cabecalho = base64Url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  return `${cabecalho}.${base64Url(JSON.stringify(payload))}.assinatura-mock`;
}

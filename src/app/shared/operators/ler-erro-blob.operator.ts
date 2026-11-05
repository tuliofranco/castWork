import { HttpErrorResponse } from '@angular/common/http';
import { type MonoTypeOperatorFunction, catchError, from, switchMap, throwError } from 'rxjs';

async function converter(erro: HttpErrorResponse): Promise<HttpErrorResponse> {
  if (!(erro.error instanceof Blob)) return erro;
  const texto = await erro.error.text();
  let corpo: unknown = texto;
  try {
    corpo = JSON.parse(texto);
  } catch {
    // Corpo não-JSON (ex.: HTML de proxy): mantém como texto.
  }
  return new HttpErrorResponse({ error: corpo, headers: erro.headers, status: erro.status, statusText: erro.statusText, url: erro.url ?? undefined });
}

/**
 * Com `responseType: 'blob'`, o corpo do ERRO também chega como Blob (o HttpClient não sabe
 * de antemão se virá PDF ou JSON). Este operador lê o Blob e devolve o erro com o JSON já parseado.
 */
export function lerErroBlob<T>(): MonoTypeOperatorFunction<T> {
  return catchError((erro: unknown) =>
    erro instanceof HttpErrorResponse
      ? from(converter(erro)).pipe(switchMap((convertido) => throwError(() => convertido)))
      : throwError(() => erro),
  );
}

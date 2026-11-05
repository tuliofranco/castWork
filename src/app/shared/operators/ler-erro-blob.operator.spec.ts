import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, throwError } from 'rxjs';

import { lerErroBlob } from './ler-erro-blob.operator';

describe('lerErroBlob', () => {
  const falharCom = (corpo: unknown) =>
    firstValueFrom(throwError(() => new HttpErrorResponse({ status: 404, error: corpo })).pipe(lerErroBlob())).catch(
      (e: unknown) => e as HttpErrorResponse,
    );

  it('deve converter Blob JSON em objeto, mantendo o status', async () => {
    const erro = await falharCom(new Blob([JSON.stringify({ title: 'Peça não encontrada' })], { type: 'application/json' }));
    expect(erro.status).toBe(404);
    expect(erro.error).toEqual({ title: 'Peça não encontrada' });
  });

  it('deve manter texto quando o Blob não é JSON', async () => {
    expect((await falharCom(new Blob(['<html>Bad Gateway</html>']))).error).toBe('<html>Bad Gateway</html>');
  });

  it('não deve mexer em erro que não é Blob', async () => {
    expect((await falharCom({ title: 'x' })).error).toEqual({ title: 'x' });
  });
});

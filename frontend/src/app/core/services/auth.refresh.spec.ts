import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LoginService } from '@data/auth/login/login.service';
import { RefreshTokenService } from '@data/auth/refresh-token/refresh-token.service';
import { criarTokenMock } from '@data/auth/login/mocks/token-payload.mock';
import { Subject, defer, firstValueFrom, of, throwError } from 'rxjs';

import { AuthService, CHAVE_ACCESS_TOKEN, CHAVE_REFRESH_TOKEN } from './auth.service';

describe('AuthService.renovarToken', () => {
  const postRefreshToken = vi.fn();
  let service: AuthService;

  beforeEach(() => {
    localStorage.clear();
    postRefreshToken.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: LoginService, useValue: {} },
        { provide: RefreshTokenService, useValue: { postRefreshToken } },
      ],
    });
    service = TestBed.inject(AuthService);
  });

  it('deve compartilhar UM refresh entre chamadas simultâneas', async () => {
    localStorage.setItem(CHAVE_REFRESH_TOKEN, 'r1');
    const resposta = new Subject<{ accessToken: string; refreshToken: string }>();
    postRefreshToken.mockReturnValue(resposta);

    const chamadas = [service.renovarToken(), service.renovarToken(), service.renovarToken()].map((o) => firstValueFrom(o));
    const novo = criarTokenMock();
    resposta.next({ accessToken: novo, refreshToken: 'r2' });
    resposta.complete();

    expect(await Promise.all(chamadas)).toEqual([novo, novo, novo]);
    expect(postRefreshToken).toHaveBeenCalledTimes(1);
    expect(postRefreshToken).toHaveBeenCalledWith({ refreshToken: 'r1' });
    expect(localStorage.getItem(CHAVE_ACCESS_TOKEN)).toBe(novo);
    expect(localStorage.getItem(CHAVE_REFRESH_TOKEN)).toBe('r2');
    expect(service.token()).toBe(novo);
  });

  it('deve permitir um NOVO refresh depois que o anterior terminou', async () => {
    localStorage.setItem(CHAVE_REFRESH_TOKEN, 'r1');
    for (const n of [1, 2]) {
      const resposta = new Subject<{ accessToken: string; refreshToken: string }>();
      postRefreshToken.mockReturnValueOnce(resposta);
      const promessa = firstValueFrom(service.renovarToken());
      resposta.next({ accessToken: `t${n}`, refreshToken: `r${n + 1}` });
      resposta.complete();
      await promessa;
    }
    expect(postRefreshToken).toHaveBeenCalledTimes(2);
  });

  it('deve falhar sem refresh token guardado', async () => {
    await expect(firstValueFrom(service.renovarToken())).rejects.toThrow('Sem refresh token');
    expect(postRefreshToken).not.toHaveBeenCalled();
  });

  /** O POST do refresh: falha com `erro` na primeira tentativa e responde na segunda. */
  function falharUmaVez(erro: HttpErrorResponse) {
    let tentativas = 0;
    postRefreshToken.mockReturnValue(defer(() => (++tentativas === 1 ? throwError(() => erro) : of({ accessToken: 'novo', refreshToken: 'r2' }))));
    return () => tentativas;
  }
  const limite = (retryAfter: string) => new HttpErrorResponse({ status: 429, headers: new HttpHeaders({ 'Retry-After': retryAfter }) });

  it('deve tentar de novo depois do Retry-After quando o refresh bate no limite de requisições', async () => {
    localStorage.setItem(CHAVE_REFRESH_TOKEN, 'r1');
    const tentativas = falharUmaVez(limite('0'));

    expect(await firstValueFrom(service.renovarToken())).toBe('novo');
    expect(tentativas()).toBe(2);
    expect(localStorage.getItem(CHAVE_REFRESH_TOKEN)).toBe('r2');
  });

  it.each([
    ['429 com espera longa', limite('30')],
    ['429 sem Retry-After', new HttpErrorResponse({ status: 429 })],
    ['queda de rede', new HttpErrorResponse({ status: 0 })],
    ['erro do servidor', new HttpErrorResponse({ status: 500 })],
  ])('não deve repetir o refresh em %s', async (_caso, erro) => {
    localStorage.setItem(CHAVE_REFRESH_TOKEN, 'r1');
    const tentativas = falharUmaVez(erro);

    await expect(firstValueFrom(service.renovarToken())).rejects.toBe(erro);
    expect(tentativas()).toBe(1);
    expect(localStorage.getItem(CHAVE_REFRESH_TOKEN)).toBe('r1');
  });
});

import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LoginService } from '@data/auth/login/login.service';
import { RefreshTokenService } from '@data/auth/refresh-token/refresh-token.service';
import { criarTokenMock } from '@data/auth/login/mocks/token-payload.mock';
import { Subject, firstValueFrom } from 'rxjs';

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
});

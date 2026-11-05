import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { LoginService } from '@data/auth/login/login.service';
import { criarTokenMock } from '@data/auth/login/mocks/token-payload.mock';
import { firstValueFrom, of } from 'rxjs';

import { AuthService, CHAVE_ACCESS_TOKEN, CHAVE_REFRESH_TOKEN } from './auth.service';

describe('AuthService', () => {
  const postLogin = vi.fn();

  function criar(): AuthService {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: LoginService, useValue: { postLogin } }],
    });
    return TestBed.inject(AuthService);
  }

  beforeEach(() => {
    localStorage.clear();
    postLogin.mockReset();
  });

  it('deve gravar os tokens no storage e no signal após o login', async () => {
    const token = criarTokenMock();
    postLogin.mockReturnValue(of({ accessToken: token, refreshToken: 'r1' }));
    const service = criar();

    await firstValueFrom(service.login({ email: 'a@b.com', senha: 'x' }));

    expect(service.token()).toBe(token);
    expect(localStorage.getItem(CHAVE_ACCESS_TOKEN)).toBe(token);
    expect(localStorage.getItem(CHAVE_REFRESH_TOKEN)).toBe('r1');
    expect(service.isLoggedIn()).toBe(true);
  });

  it('deve restaurar a sessão do storage (F5)', () => {
    localStorage.setItem(CHAVE_ACCESS_TOKEN, criarTokenMock());
    expect(criar().isLoggedIn()).toBe(true);
  });

  it('não deve considerar logado com token vencido no storage', () => {
    localStorage.setItem(CHAVE_ACCESS_TOKEN, criarTokenMock({ exp: Math.floor(Date.now() / 1000) - 60 }));
    expect(criar().isLoggedIn()).toBe(false);
  });

  it('não deve considerar logado sem token', () => {
    expect(criar().isLoggedIn()).toBe(false);
  });

  it('deve limpar a sessão e ir para /login com returnTo no logout', () => {
    localStorage.setItem(CHAVE_ACCESS_TOKEN, criarTokenMock());
    const service = criar();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    service.logout('/pecas');

    expect(service.token()).toBeNull();
    expect(localStorage.getItem(CHAVE_ACCESS_TOKEN)).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnTo: '/pecas' } });
  });
});

import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { CHAVE_ACCESS_TOKEN, CHAVE_REFRESH_TOKEN } from '@core/services/auth.service';
import { RefreshTokenService } from '@data/auth/refresh-token/refresh-token.service';
import { of, throwError } from 'rxjs';
import { criarTokenMock } from '@data/auth/login/mocks/token-payload.mock';

import { authGuard } from './auth.guard';
import { convidadoGuard } from './convidado.guard';
import { permissionGuard } from './permission.guard';

@Component({ template: '' })
class VazioComponent {}

describe('Guards', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'login', component: VazioComponent, canActivate: [convidadoGuard] },
          { path: '403', component: VazioComponent },
          {
            path: '',
            canActivate: [authGuard],
            canActivateChild: [permissionGuard],
            children: [
              { path: 'pecas', component: VazioComponent, data: { permissoes: ['pecas.ler'] } },
              { path: 'livre', component: VazioComponent },
            ],
          },
        ]),
      ],
    });
  });

  const logar = (permissoes: string[], exp = Math.floor(Date.now() / 1000) + 3600) =>
    localStorage.setItem(CHAVE_ACCESS_TOKEN, criarTokenMock({ permissoes, exp }));
  const url = () => TestBed.inject(Router).url;

  describe('authGuard', () => {
    it('deve redirecionar para /login com returnTo quando não logado', async () => {
      await RouterTestingHarness.create('/pecas?status=producao');
      expect(url()).toBe('/login?returnTo=%2Fpecas%3Fstatus%3Dproducao');
    });

    const vencido = () => logar(['pecas.ler'], Math.floor(Date.now() / 1000) - 60);
    const comRefresh = (postRefreshToken: () => unknown) => {
      vencido();
      localStorage.setItem(CHAVE_REFRESH_TOKEN, 'refresh-guardado');
      TestBed.overrideProvider(RefreshTokenService, { useValue: { postRefreshToken } });
    };

    it('deve renovar a sessão e seguir quando o token venceu mas há refresh token', async () => {
      const novo = criarTokenMock({ permissoes: ['pecas.ler'], exp: Math.floor(Date.now() / 1000) + 3600 });
      comRefresh(() => of({ accessToken: novo, refreshToken: 'refresh-novo' }));

      await RouterTestingHarness.create('/pecas');

      expect(url()).toBe('/pecas');
      expect(localStorage.getItem(CHAVE_REFRESH_TOKEN)).toBe('refresh-novo');
    });

    it('deve ir ao login e limpar a sessão quando o refresh é recusado', async () => {
      comRefresh(() => throwError(() => new HttpErrorResponse({ status: 401 })));

      await RouterTestingHarness.create('/pecas');

      expect(url()).toBe('/login?returnTo=%2Fpecas');
      expect(localStorage.getItem(CHAVE_REFRESH_TOKEN)).toBeNull();
    });

    it('deve ir ao login sem jogar fora o refresh token quando a renovação falha por rede', async () => {
      comRefresh(() => throwError(() => new HttpErrorResponse({ status: 0 })));

      await RouterTestingHarness.create('/pecas');

      expect(url()).toBe('/login?returnTo=%2Fpecas');
      expect(localStorage.getItem(CHAVE_REFRESH_TOKEN)).toBe('refresh-guardado');
    });

    it('deve redirecionar e limpar o storage quando o token venceu e não há refresh token', async () => {
      logar(['pecas.ler'], Math.floor(Date.now() / 1000) - 60);
      await RouterTestingHarness.create('/pecas');
      expect(url()).toMatch(/^\/login/);
      expect(localStorage.getItem(CHAVE_ACCESS_TOKEN)).toBeNull();
    });
  });

  describe('permissionGuard', () => {
    it('deve liberar quem tem a permissão', async () => {
      logar(['pecas.ler']);
      await RouterTestingHarness.create('/pecas');
      expect(url()).toBe('/pecas');
    });

    it('deve mandar para /403 quem não tem a permissão', async () => {
      logar(['clientes.ler']);
      await RouterTestingHarness.create('/pecas');
      expect(url()).toBe('/403');
    });

    it('deve liberar rota sem permissoes declaradas', async () => {
      logar([]);
      await RouterTestingHarness.create('/livre');
      expect(url()).toBe('/livre');
    });
  });

  describe('convidadoGuard', () => {
    it('deve tirar da tela de login quem já está logado', async () => {
      logar(['pecas.ler']);
      await RouterTestingHarness.create('/login');
      expect(url()).not.toBe('/login');
    });

    it('deve deixar entrar no login quem não está logado', async () => {
      await RouterTestingHarness.create('/login');
      expect(url()).toBe('/login');
    });
  });
});

import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { JwtHelperService } from '@auth0/angular-jwt';
import type { TokenPayload } from '@core/models/token-payload.model';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { firstValueFrom } from 'rxjs';

import { LoginService } from './login.service';
import { SENHA_DEMO } from './mocks/usuarios-demo.mock';

function configurar(usarMocks: boolean): LoginService {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: AMBIENTE, useValue: { production: false, apiUrl: '', graphqlUrl: '', usarMocks } },
    ],
  });
  return TestBed.inject(LoginService);
}

describe('LoginService', () => {
  describe('com API real', () => {
    it('deve fazer POST em auth/login com o DTO', () => {
      const service = configurar(false);
      const http = TestBed.inject(HttpTestingController);
      const resposta = { accessToken: 'a', refreshToken: 'r' };
      let recebido: unknown;

      service.postLogin({ email: 'a@b.com', senha: 'x' }).subscribe((r) => (recebido = r));

      const req = http.expectOne('auth/login');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'a@b.com', senha: 'x' });
      req.flush(resposta);
      expect(recebido).toEqual(resposta);
      http.verify();
    });
  });

  describe('com mock', () => {
    it('deve emitir um JWT decodificável com as claims do usuário demo', async () => {
      const service = configurar(true);
      const { accessToken } = await firstValueFrom(
        service.postLogin({ email: 'Aprendiz@castwork.dev', senha: SENHA_DEMO }),
      );
      const payload = new JwtHelperService().decodeToken<TokenPayload>(accessToken);
      expect(payload?.nome).toBe('Caio Aprendiz');
      expect(payload?.permissoes).toEqual(['pecas.ler', 'portfolio.ler']);
      expect(payload!.exp).toBeGreaterThan(Date.now() / 1000);
    });

    it('deve responder 401 com senha errada', async () => {
      const service = configurar(true);
      const erro = await firstValueFrom(service.postLogin({ email: 'joalheira@castwork.dev', senha: 'errada' })).catch(
        (e: unknown) => e,
      );
      expect(erro).toBeInstanceOf(HttpErrorResponse);
      expect((erro as HttpErrorResponse).status).toBe(401);
    });
  });
});

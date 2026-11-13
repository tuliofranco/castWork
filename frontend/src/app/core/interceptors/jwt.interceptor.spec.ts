import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import type { HttpTestingController } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { semAuth } from '@shared/injection-tokens/http-context.tokens';
import { Subject, throwError } from 'rxjs';

import { jwtInterceptor } from './jwt.interceptor';
import { configurarHttp } from './testing';

const API = 'https://api.teste/api';

describe('jwtInterceptor', () => {
  const token = signal<string | null>('token-velho');
  const auth = { token, renovarToken: vi.fn(), logout: vi.fn() };
  let http: HttpTestingController;
  let cliente: HttpClient;

  beforeEach(() => {
    token.set('token-velho');
    auth.renovarToken.mockReset();
    auth.logout.mockReset();
    http = configurarHttp([jwtInterceptor], [provideRouter([]), { provide: AuthService, useValue: auth }]);
    cliente = TestBed.inject(HttpClient);
  });

  afterEach(() => http.verify());

  it('deve anexar o Bearer nas chamadas à nossa API', () => {
    cliente.get(`${API}/pecas`).subscribe();
    expect(http.expectOne(`${API}/pecas`).request.headers.get('Authorization')).toBe('Bearer token-velho');
  });

  it('NÃO deve enviar o token para outros domínios', () => {
    cliente.get('https://cdn.terceiro.com/x').subscribe();
    expect(http.expectOne('https://cdn.terceiro.com/x').request.headers.has('Authorization')).toBe(false);
  });

  it('deve respeitar BYPASS_AUTH', () => {
    cliente.get(`${API}/auth/login`, { context: semAuth() }).subscribe();
    expect(http.expectOne(`${API}/auth/login`).request.headers.has('Authorization')).toBe(false);
  });

  it('no 401 deve renovar e repetir com o token novo', () => {
    const renovado = new Subject<string>();
    auth.renovarToken.mockReturnValue(renovado);
    let resposta: unknown;
    cliente.get(`${API}/pecas`).subscribe((r) => (resposta = r));

    http.expectOne(`${API}/pecas`).flush(null, { status: 401, statusText: 'Unauthorized' });
    renovado.next('token-novo');
    renovado.complete();

    const repetida = http.expectOne(`${API}/pecas`);
    expect(repetida.request.headers.get('Authorization')).toBe('Bearer token-novo');
    repetida.flush({ ok: true });
    expect(resposta).toEqual({ ok: true });
  });

  it.each([
    ['sem refresh token', new Error('Sem refresh token')],
    ['refresh recusado (401)', new HttpErrorResponse({ status: 401 })],
    ['refresh inválido (400)', new HttpErrorResponse({ status: 400 })],
  ])('se o refresh falhar de vez (%s), deve deslogar e propagar o 401 original', (_caso, falha) => {
    auth.renovarToken.mockReturnValue(throwError(() => falha));
    let erro: unknown;
    cliente.get(`${API}/pecas`).subscribe({ error: (e: unknown) => (erro = e) });

    http.expectOne(`${API}/pecas`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).toHaveBeenCalledWith('/');
    expect((erro as HttpErrorResponse).status).toBe(401);
  });

  it.each([0, 429, 500, 503])('se o refresh falhar por rede ou servidor (%i), deve manter a sessão e propagar esse erro', (status) => {
    auth.renovarToken.mockReturnValue(throwError(() => new HttpErrorResponse({ status })));
    let erro: unknown;
    cliente.get(`${API}/pecas`).subscribe({ error: (e: unknown) => (erro = e) });

    http.expectOne(`${API}/pecas`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).not.toHaveBeenCalled();
    expect((erro as HttpErrorResponse).status).toBe(status);
  });

  it('não deve tentar refresh para erros diferentes de 401', () => {
    cliente.get(`${API}/pecas`).subscribe({ error: () => undefined });
    http.expectOne(`${API}/pecas`).flush(null, { status: 403, statusText: 'Forbidden' });
    expect(auth.renovarToken).not.toHaveBeenCalled();
  });

  it('deve repetir no máximo uma vez (401 depois do refresh não renova de novo)', () => {
    auth.renovarToken.mockReturnValue(new Subject<string>().asObservable());
    const renovado = new Subject<string>();
    auth.renovarToken.mockReturnValue(renovado);
    let erro: unknown;
    cliente.get(`${API}/pecas`).subscribe({ error: (e: unknown) => (erro = e) });

    http.expectOne(`${API}/pecas`).flush(null, { status: 401, statusText: 'Unauthorized' });
    renovado.next('token-novo');
    http.expectOne(`${API}/pecas`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.renovarToken).toHaveBeenCalledTimes(1);
    expect((erro as HttpErrorResponse).status).toBe(401);
  });
});

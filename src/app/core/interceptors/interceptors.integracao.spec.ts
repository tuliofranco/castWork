import { HttpClient } from '@angular/common/http';
import type { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CHAVE_ACCESS_TOKEN, CHAVE_REFRESH_TOKEN } from '@core/services/auth.service';
import { criarTokenMock } from '@data/auth/login/mocks/token-payload.mock';
import { criarHttpParams } from '@data/common/http/criar-http-params';
import { LoadingService } from '@shared/services/loading.service';
import { ToastService } from '@shared/services/toast.service';

import { INTERCEPTORS } from '.';
import { configurarHttp } from './testing';

/** Cadeia completa, na ordem real: prova que as peças funcionam juntas. */
describe('Cadeia de interceptors (integração)', () => {
  let http: HttpTestingController;
  let cliente: HttpClient;

  beforeEach(() => {
    localStorage.setItem(CHAVE_ACCESS_TOKEN, criarTokenMock());
    localStorage.setItem(CHAVE_REFRESH_TOKEN, 'r1');
    http = configurarHttp(INTERCEPTORS, [provideRouter([])]);
    cliente = TestBed.inject(HttpClient);
  });

  afterEach(() => localStorage.clear());

  it('deve montar URL, limpar nulos, formatar data e anexar o token', () => {
    cliente.get('pecas', { params: criarHttpParams({ termo: null, desde: new Date(2026, 10, 5) }) }).subscribe();
    const req = http.expectOne((r) => r.url === 'https://api.teste/api/pecas');
    expect(req.request.urlWithParams).toBe('https://api.teste/api/pecas?desde=2026-11-05');
    expect(req.request.headers.get('Authorization')).toMatch(/^Bearer /);
    req.flush([]);
  });

  it('com 3 requisições recebendo 401 juntas, deve fazer UM único refresh', () => {
    const toast = TestBed.inject(ToastService);
    const respostas: unknown[] = [];
    for (const recurso of ['a', 'b', 'c']) cliente.get(recurso).subscribe((r) => respostas.push(r));

    for (const recurso of ['a', 'b', 'c']) {
      http.expectOne(`https://api.teste/api/${recurso}`).flush(null, { status: 401, statusText: 'Unauthorized' });
    }

    const refreshes = http.match('https://api.teste/api/auth/refresh');
    expect(refreshes).toHaveLength(1);
    refreshes[0]!.flush({ accessToken: criarTokenMock({ nome: 'Renovado' }), refreshToken: 'r2' });

    for (const recurso of ['a', 'b', 'c']) http.expectOne(`https://api.teste/api/${recurso}`).flush(recurso);

    expect(respostas.sort()).toEqual(['a', 'b', 'c']);
    expect(localStorage.getItem(CHAVE_REFRESH_TOKEN)).toBe('r2');
    expect(toast.toasts()).toHaveLength(0); // 401 resolvido não vira toast
    expect(TestBed.inject(LoadingService).ativo()).toBe(false);
    http.verify();
  });
});

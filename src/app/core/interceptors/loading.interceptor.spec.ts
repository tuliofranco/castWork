import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { semLoading } from '@shared/injection-tokens/http-context.tokens';
import { LoadingService } from '@shared/services/loading.service';

import { loadingInterceptor } from './loading.interceptor';
import { configurarHttp } from './testing';

describe('loadingInterceptor', () => {
  it('deve ficar ativo até a ÚLTIMA requisição terminar', () => {
    const http = configurarHttp([loadingInterceptor]);
    const cliente = TestBed.inject(HttpClient);
    const loading = TestBed.inject(LoadingService);

    cliente.get('a').subscribe();
    cliente.get('b').subscribe({ error: () => undefined });
    expect(loading.ativo()).toBe(true);

    http.expectOne('a').flush({});
    expect(loading.ativo()).toBe(true);
    http.expectOne('b').flush(null, { status: 500, statusText: 'Erro' });
    expect(loading.ativo()).toBe(false);
  });

  it('deve encerrar quando a requisição é cancelada (unsubscribe)', () => {
    configurarHttp([loadingInterceptor]);
    const assinatura = TestBed.inject(HttpClient).get('a').subscribe();
    assinatura.unsubscribe();
    expect(TestBed.inject(LoadingService).ativo()).toBe(false);
  });

  it('deve respeitar BYPASS_LOADING', () => {
    const http = configurarHttp([loadingInterceptor]);
    TestBed.inject(HttpClient).get('a', { context: semLoading() }).subscribe();
    expect(TestBed.inject(LoadingService).ativo()).toBe(false);
    http.expectOne('a').flush({});
  });
});

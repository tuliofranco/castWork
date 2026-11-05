import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { semUrlBase } from '@shared/injection-tokens/http-context.tokens';

import { AMBIENTE_TESTE, configurarHttp } from './testing';
import { urlBaseInterceptor } from './url-base.interceptor';

describe('urlBaseInterceptor', () => {
  it.each([
    ['pecas', 'https://api.teste/api/pecas'],
    ['/pecas/1', 'https://api.teste/api/pecas/1'],
    ['https://outro.host/x', 'https://outro.host/x'],
  ])('%s → %s', (url, esperada) => {
    const http = configurarHttp([urlBaseInterceptor]);
    TestBed.inject(HttpClient).get(url).subscribe();
    http.expectOne(esperada).flush({});
    http.verify();
  });

  it('não deve prefixar o endpoint GraphQL relativo (ambiente com proxy)', () => {
    const http = configurarHttp([urlBaseInterceptor], [
      { provide: AMBIENTE, useValue: { ...AMBIENTE_TESTE, apiUrl: '/api', graphqlUrl: '/graphql' } },
    ]);
    TestBed.inject(HttpClient).post('/graphql', {}).subscribe();
    http.expectOne('/graphql').flush({});
    http.verify();
  });

  it('deve respeitar BYPASS_URL_BASE', () => {
    const http = configurarHttp([urlBaseInterceptor]);
    TestBed.inject(HttpClient).get('assets/config.json', { context: semUrlBase() }).subscribe();
    http.expectOne('assets/config.json').flush({});
  });
});

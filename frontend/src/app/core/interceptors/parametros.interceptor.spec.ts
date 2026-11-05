import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { criarHttpParams } from '@data/common/http/criar-http-params';

import { removerParametrosNulosInterceptor } from './remover-parametros-nulos.interceptor';
import { serializarData, serializarDatasInterceptor } from './serializar-datas.interceptor';
import { configurarHttp } from './testing';

describe('Interceptors de parâmetros', () => {
  it('deve remover null, undefined e vazio, mantendo 0 e false', () => {
    const http = configurarHttp([removerParametrosNulosInterceptor]);
    const params = criarHttpParams({ termo: '', status: undefined, clienteId: null, pagina: 0, decrescente: false });
    TestBed.inject(HttpClient).get('pecas', { params }).subscribe();

    const req = http.expectOne((r) => r.url === 'pecas');
    expect(req.request.params.keys()).toEqual(['pagina', 'decrescente']);
    expect(req.request.urlWithParams).toBe('pecas?pagina=0&decrescente=false');
  });

  it('deve limpar itens nulos de parâmetros com vários valores', () => {
    const http = configurarHttp([removerParametrosNulosInterceptor]);
    const params = criarHttpParams({ status: ['producao', null, 'acabamento'] });
    TestBed.inject(HttpClient).get('pecas', { params }).subscribe();
    expect(http.expectOne((r) => r.url === 'pecas').request.params.getAll('status')).toEqual(['producao', 'acabamento']);
  });

  it('deve serializar Date nos query params', () => {
    const http = configurarHttp([serializarDatasInterceptor]);
    const params = criarHttpParams({ desde: new Date(2026, 10, 5), termo: 'anel' });
    TestBed.inject(HttpClient).get('pecas', { params }).subscribe();

    const req = http.expectOne((r) => r.url === 'pecas');
    expect(req.request.params.get('desde')).toBe('2026-11-05');
    expect(req.request.params.get('termo')).toBe('anel');
  });

  it('não deve recriar a requisição quando não há o que mudar', () => {
    const http = configurarHttp([removerParametrosNulosInterceptor, serializarDatasInterceptor]);
    const params = criarHttpParams({ termo: 'anel' });
    TestBed.inject(HttpClient).get('pecas', { params }).subscribe();
    expect(http.expectOne((r) => r.url === 'pecas').request.params).toBe(params);
  });

  describe('serializarData', () => {
    it('meia-noite local vira yyyy-MM-dd (filtro de dia, sem deslocar fuso)', () => {
      expect(serializarData(new Date(2026, 0, 9))).toBe('2026-01-09');
    });

    it('data com horário vira ISO em UTC', () => {
      const data = new Date(2026, 0, 9, 14, 30);
      expect(serializarData(data)).toBe(data.toISOString());
    });
  });
});

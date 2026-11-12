import { HttpEventType, HttpHeaders, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { FerramentasService } from './ferramentas.service';

describe('FerramentasService', () => {
  function configurar() {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { service: TestBed.inject(FerramentasService), http: TestBed.inject(HttpTestingController) };
  }

  it('deve montar a rota pelo escopo: versão, peça do projeto ou projeto', () => {
    const { service, http } = configurar();

    service.putArquivo(1, 2, 'desenho-2d', 'eA==').subscribe();
    service.putArquivo(1, { pecaId: 5 }, 'modelo-3d', 'eA==').subscribe();
    service.getConversa(1, null, 'briefing').subscribe();

    http.expectOne((r) => r.method === 'PUT' && r.url === 'projetos/1/versoes/2/ferramentas/desenho-2d/arquivo').flush({ id: 10 });
    http.expectOne((r) => r.method === 'PUT' && r.url === 'projetos/1/pecas/5/ferramentas/modelo-3d/arquivo').flush({ id: 11 });
    http.expectOne('projetos/1/ferramentas/briefing/conversa').flush({ mensagens: [], arquivoId: null, processando: false, chat: 1, chatsAnteriores: 0 });
    expect(() => http.verify()).not.toThrow();
  });

  it('deve montar o caminho da imagem pelo escopo', () => {
    const { service } = configurar();

    expect(service.caminhoDaImagem(1, 2, 'abc')).toBe('projetos/1/versoes/2/ferramentas/imagens/abc');
    expect(service.caminhoDaImagem(1, { pecaId: 5 }, 'abc')).toBe('projetos/1/pecas/5/ferramentas/imagens/abc');
    expect(service.caminhoDaImagem(1, null, 'abc')).toBe('projetos/1/ferramentas/imagens/abc');
  });

  it('deve devolver o arquivo com o Id do cabeçalho, nulo no 204 e falhar sem o cabeçalho', () => {
    const { service, http } = configurar();
    const url = 'projetos/1/versoes/2/ferramentas/desenho-2d/arquivo';
    const resultados: unknown[] = [];
    const pedir = () => service.getArquivo(1, 2, 'desenho-2d').subscribe({ next: (a) => resultados.push(a?.id ?? null), error: (e: Error) => resultados.push(e.message) });

    pedir();
    http.expectOne(url).flush(new Blob(['dxf']), { headers: new HttpHeaders({ 'X-Arquivo-Id': '42' }) });
    pedir();
    http.expectOne(url).flush(null, { status: 204, statusText: 'No Content' });
    pedir();
    http.expectOne(url).flush(new Blob(['dxf']));

    expect(resultados).toEqual([42, null, 'A resposta do arquivo veio sem o identificador (X-Arquivo-Id).']);
  });

  it('deve ignorar um bloco inválido no ao vivo, sem repetir eventos nem perder o fim', () => {
    const { service, http } = configurar();
    const tipos: string[] = [];
    let terminou = false;
    service.acompanhar(1, 2, 'desenho-2d', false).subscribe({ next: (e) => tipos.push(e.tipo), complete: () => (terminou = true) });
    const pedido = http.expectOne('projetos/1/versoes/2/ferramentas/desenho-2d/conversa/eventos');
    const progresso = (partialText: string) => pedido.event({ type: HttpEventType.DownloadProgress, loaded: partialText.length, partialText });

    const inicio = 'data: {"tipo":"texto","texto":"Oi"}\n\ndata: <html>502 Bad Gateway</html>\n\n';
    progresso(inicio);
    progresso(inicio + 'data: {"tipo":"fim"}\n\n');

    expect(tipos).toEqual(['texto', 'fim']);
    expect(terminou).toBe(true);
  });
});

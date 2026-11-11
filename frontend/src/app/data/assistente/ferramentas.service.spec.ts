import { provideHttpClient } from '@angular/common/http';
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
});

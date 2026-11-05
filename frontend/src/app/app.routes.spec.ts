import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { CHAVE_ACCESS_TOKEN } from '@core/services/auth.service';
import { criarTokenMock } from '@data/auth/login/mocks/token-payload.mock';

import { appConfig } from './app.config';

describe('Rotas da aplicação', () => {
  beforeEach(() => {
    localStorage.setItem(
      CHAVE_ACCESS_TOKEN,
      criarTokenMock({
        permissoes: ['projetos.ler', 'clientes.ler', 'pecas.ler', 'precificacao.ler', 'documentos.ler', 'portfolio.ler'],
      }),
    );
    TestBed.configureTestingModule({ providers: appConfig.providers });
  });

  afterEach(() => localStorage.clear());

  it('deve redirecionar a raiz para /projetos, a primeira aba', async () => {
    await RouterTestingHarness.create('/');
    expect(TestBed.inject(Router).url).toBe('/projetos');
  });

  it.each([
    ['/projetos', 'Projetos'],
    ['/pecas', 'Peças'],
    ['/clientes', 'Clientes'],
    ['/precificacao', 'Precificação'],
    ['/documentos', 'Documentos'],
    ['/portfolio', 'Portfólio'],
  ])('deve carregar %s sob demanda com o título correto', async (url, titulo) => {
    const harness = await RouterTestingHarness.create(url);
    expect(harness.routeNativeElement!.querySelector('h1')?.textContent).toBe(titulo);
    expect(TestBed.inject(Title).getTitle()).toBe(`${titulo} | Castwork`);
  });

  it('deve mandar para o login quem não está logado', async () => {
    localStorage.clear();
    await RouterTestingHarness.create('/clientes');
    expect(TestBed.inject(Router).url).toBe('/login?returnTo=%2Fclientes');
    expect(TestBed.inject(Title).getTitle()).toBe('Entrar | Castwork');
  });

  it('deve mostrar 404 para rota inexistente', async () => {
    const harness = await RouterTestingHarness.create('/nao-existe');
    expect(harness.routeNativeElement!.textContent).toContain('404');
    expect(TestBed.inject(Title).getTitle()).toBe('Página não encontrada | Castwork');
  });
});

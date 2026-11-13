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

  it('deve abrir na primeira aba permitida quem não pode ver projetos', async () => {
    localStorage.setItem(CHAVE_ACCESS_TOKEN, criarTokenMock({ permissoes: ['pecas.ler', 'portfolio.ler'] }));
    await RouterTestingHarness.create('/');
    expect(TestBed.inject(Router).url).toBe('/pecas');
  });

  it('deve mostrar o 403 com Sair a quem não pode ver nenhuma página', async () => {
    localStorage.setItem(CHAVE_ACCESS_TOKEN, criarTokenMock({ permissoes: [] }));
    const harness = await RouterTestingHarness.create('/');
    expect(TestBed.inject(Router).url).toBe('/403');
    expect(harness.fixture.nativeElement.textContent).toContain('Seu acesso ainda não inclui nenhuma página.');
    expect([...harness.fixture.nativeElement.querySelectorAll('button')].map((b: HTMLButtonElement) => b.textContent?.trim())).toContain('Sair');
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

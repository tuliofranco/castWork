import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AuthService } from '@core/services/auth.service';
import { NEVER, of, throwError } from 'rxjs';

import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  const login = vi.fn();
  let harness: RouterTestingHarness;

  async function abrir(url = '/login'): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', component: LoginComponent }, { path: '**', children: [] }], withComponentInputBinding()),
        { provide: AuthService, useValue: { login } },
      ],
    });
    harness = await RouterTestingHarness.create(url);
    return harness.routeNativeElement!;
  }

  async function preencherEEnviar(el: HTMLElement, email: string, senha: string): Promise<void> {
    const campo = (id: string) => el.querySelector<HTMLInputElement>(`#${id}`)!;
    campo('email').value = email;
    campo('email').dispatchEvent(new Event('input'));
    campo('senha').value = senha;
    campo('senha').dispatchEvent(new Event('input'));
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    await harness.fixture.whenStable();
  }

  beforeEach(() => login.mockReset());

  it('não deve chamar o login com formulário inválido e deve exibir os erros', async () => {
    const el = await abrir();
    await preencherEEnviar(el, 'nao-e-email', '');
    expect(login).not.toHaveBeenCalled();
    expect(el.textContent).toContain('E-mail inválido.');
    expect(el.textContent).toContain('Informe a senha.');
  });

  it('deve entrar e voltar para o returnTo', async () => {
    login.mockReturnValue(of(undefined));
    const el = await abrir('/login?returnTo=%2Fpecas%3Fpagina%3D2');
    await preencherEEnviar(el, 'joalheira@castwork.dev', 'castwork');
    expect(login).toHaveBeenCalledWith({ email: 'joalheira@castwork.dev', senha: 'castwork' });
    expect(TestBed.inject(Router).url).toBe('/pecas?pagina=2');
  });

  it('deve ignorar returnTo externo', async () => {
    login.mockReturnValue(of(undefined));
    const el = await abrir('/login?returnTo=https%3A%2F%2Fsite-falso.com');
    await preencherEEnviar(el, 'a@b.com', 'x');
    expect(TestBed.inject(Router).url).toBe('/');
  });

  it('deve mostrar mensagem de credenciais inválidas no 401', async () => {
    login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    const el = await abrir();
    await preencherEEnviar(el, 'a@b.com', 'errada');
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('E-mail ou senha inválidos.');
  });

  it('deve mostrar mensagem genérica para outros erros', async () => {
    login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    const el = await abrir();
    await preencherEEnviar(el, 'a@b.com', 'x');
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Não foi possível entrar.');
  });

  it('deve desabilitar o botão enquanto envia', async () => {
    login.mockReturnValue(NEVER);
    const el = await abrir();
    await preencherEEnviar(el, 'a@b.com', 'x');
    const botao = el.querySelector<HTMLButtonElement>('button[type="submit"]')!;
    expect(botao.disabled).toBe(true);
    expect(botao.textContent).toContain('Entrando...');
  });
});

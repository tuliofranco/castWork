import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { AuthService } from '@core/services/auth.service';

import { PaginaErroComponent } from './pagina-erro.component';

describe('PaginaErroComponent', () => {
  const auth = { isLoggedIn: vi.fn(() => false), logout: vi.fn() };

  it('deve oferecer Sair a quem está logado', async () => {
    auth.isLoggedIn.mockReturnValue(true);
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        provideRouter([{ path: '403', component: PaginaErroComponent, data: { codigo: '403', mensagem: 'Sem permissão' } }], withComponentInputBinding()),
      ],
    });
    const harness = await RouterTestingHarness.create('/403');

    harness.routeNativeElement!.querySelector<HTMLButtonElement>('button')!.click();

    expect(auth.logout).toHaveBeenCalledTimes(1);
  });

  it('deve exibir código e mensagem vindos do data da rota', async () => {
    auth.isLoggedIn.mockReturnValue(false);
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        provideRouter(
          [{ path: '403', component: PaginaErroComponent, data: { codigo: '403', mensagem: 'Sem permissão' } }],
          withComponentInputBinding(),
        ),
      ],
    });
    const harness = await RouterTestingHarness.create('/403');
    const texto = harness.routeNativeElement!.textContent;
    expect(texto).toContain('403');
    expect(texto).toContain('Sem permissão');
    expect(harness.routeNativeElement!.querySelector('button')).toBeNull();
  });
});

import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { PaginaErroComponent } from './pagina-erro.component';

describe('PaginaErroComponent', () => {
  it('deve exibir código e mensagem vindos do data da rota', async () => {
    TestBed.configureTestingModule({
      providers: [
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
  });
});

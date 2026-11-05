import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApolloTestingController, ApolloTestingModule } from 'apollo-angular/testing';
import { firstValueFrom } from 'rxjs';

import { ClientesService, GET_CLIENTES_OPCOES } from './clientes.service';

describe('ClientesService', () => {
  function configurar() {
    TestBed.configureTestingModule({
      imports: [ApolloTestingModule],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    return {
      service: TestBed.inject(ClientesService),
      apollo: TestBed.inject(ApolloTestingController),
      http: TestBed.inject(HttpTestingController),
    };
  }

  it('deve listar só as opções (id e nome) para o seletor', async () => {
    const { service, apollo } = configurar();
    const promessa = firstValueFrom(service.getClientesOpcoes());

    apollo.expectOne(GET_CLIENTES_OPCOES).flush({ data: { clientes: { nodes: [{ id: 1, nome: 'Ana' }, { id: 2, nome: 'Bia' }] } } });

    expect(await promessa).toEqual([{ id: 1, nome: 'Ana' }, { id: 2, nome: 'Bia' }]);
  });

  it('deve cadastrar o cliente pelo REST', async () => {
    const { service, http } = configurar();
    const promessa = firstValueFrom(service.postCliente({ nome: 'Ana', documento: null }));

    const req = http.expectOne('clientes');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 3 }, { status: 201, statusText: 'Created' });

    expect(await promessa).toEqual({ id: 3 });
  });
});

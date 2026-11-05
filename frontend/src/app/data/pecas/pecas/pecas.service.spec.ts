import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApolloTestingController, ApolloTestingModule } from 'apollo-angular/testing';
import { firstValueFrom } from 'rxjs';

import { GET_PECAS, PecasService } from './pecas.service';

describe('PecasService', () => {
  function configurar() {
    TestBed.configureTestingModule({
      imports: [ApolloTestingModule],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    return {
      service: TestBed.inject(PecasService),
      apollo: TestBed.inject(ApolloTestingController),
      http: TestBed.inject(HttpTestingController),
    };
  }

  it('deve consultar as peças com termo e cursor e devolver a página', async () => {
    const { service, apollo } = configurar();
    const promessa = firstValueFrom(service.getPecas({ termo: 'anel', primeiros: 20, apos: 'abc' }));

    const op = apollo.expectOne(GET_PECAS);
    expect(op.operation.variables).toEqual({ termo: 'anel', primeiros: 20, apos: 'abc' });
    const pagina = {
      totalCount: 1,
      pageInfo: { hasNextPage: false, endCursor: 'xyz' },
      nodes: [{ id: 1, titulo: 'Anel', status: 'BRIEFING', prazoEntrega: null, criadoEm: '2026-11-05T13:00:00Z', cliente: { id: 7, nome: 'Maria' } }],
    };
    op.flush({ data: { pecas: pagina } });

    expect(await promessa).toEqual(pagina);
  });

  it('deve criar a peça pelo REST e devolver o Id', async () => {
    const { service, http } = configurar();
    const promessa = firstValueFrom(service.postPeca({ clienteId: 7, titulo: 'Anel' }));

    const req = http.expectOne('pecas');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ clienteId: 7, titulo: 'Anel' });
    req.flush({ id: 42 }, { status: 201, statusText: 'Created' });

    expect(await promessa).toEqual({ id: 42 });
  });
});

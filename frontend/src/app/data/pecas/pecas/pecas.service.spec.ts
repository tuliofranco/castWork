import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApolloTestingController, ApolloTestingModule } from 'apollo-angular/testing';
import { firstValueFrom } from 'rxjs';

import { GET_PECAS, PecasService } from './pecas.service';

describe('PecasService', () => {
  function configurar() {
    TestBed.configureTestingModule({ imports: [ApolloTestingModule], providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { service: TestBed.inject(PecasService), apollo: TestBed.inject(ApolloTestingController), http: TestBed.inject(HttpTestingController) };
  }

  const vazia = { totalCount: 0, pageInfo: { hasNextPage: false, endCursor: null }, nodes: [] };

  it('deve mandar filtro nulo (sem filtro) por padrão e filtrar ativas só quando pedido', async () => {
    const { service, apollo } = configurar();

    const todas = firstValueFrom(service.getPecas({ termo: 'elo', primeiros: 20 }));
    const todasOp = apollo.expectOne(GET_PECAS);
    todasOp.flush({ data: { pecas: vazia } });
    await todas;
    const ativas = firstValueFrom(service.getPecas({ primeiros: 10, somenteAtivas: true }));
    const ativasOp = apollo.expectOne(GET_PECAS);
    ativasOp.flush({ data: { pecas: vazia } });
    await ativas;

    expect(todasOp.operation.variables).toEqual({ termo: 'elo', primeiros: 20, apos: null, filtro: null });
    expect(ativasOp.operation.variables).toEqual({ termo: null, primeiros: 10, apos: null, filtro: { ativo: { eq: true } } });
  });

  it('deve cadastrar a peça pelo REST', async () => {
    const { service, http } = configurar();
    const promessa = firstValueFrom(service.postPeca({ nome: 'Elo', tipo: 'ELO', teor: '18k' }));

    const req = http.expectOne('pecas');
    expect(req.request.body).toEqual({ nome: 'Elo', tipo: 'ELO', teor: '18k' });
    req.flush({ id: 5 }, { status: 201, statusText: 'Created' });

    expect(await promessa).toEqual({ id: 5 });
  });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApolloTestingController, ApolloTestingModule } from 'apollo-angular/testing';
import { firstValueFrom } from 'rxjs';

import { GET_PECA, GET_PECAS, PecasService } from './pecas.service';

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

  it('deve pedir o histórico de uso e o valor só quando informado', async () => {
    const { service, apollo } = configurar();

    const peca = firstValueFrom(service.getPeca(7, { comHistorico: true, comValor: false }));
    const op = apollo.expectOne(GET_PECA);
    op.flush({ data: { peca: null } });

    expect(await peca).toBeNull();
    expect(op.operation.variables).toEqual({ id: 7, comHistorico: true, comValor: false });
  });

  it('deve buscar o desenho da peça pela rota da biblioteca e devolver nulo sem desenho', async () => {
    const { service, http } = configurar();

    const comDesenho = firstValueFrom(service.getDesenho(7, 'modelo-3d'));
    http.expectOne('pecas/7/desenhos/modelo-3d').flush(new Blob(['{}']));
    const semDesenho = firstValueFrom(service.getDesenho(7, 'desenho-2d'));
    http.expectOne('pecas/7/desenhos/desenho-2d').flush(null, { status: 204, statusText: 'No Content' });

    expect(await comDesenho).toBeInstanceOf(Blob);
    expect(await semDesenho).toBeNull();
  });
});

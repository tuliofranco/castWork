import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApolloTestingController, ApolloTestingModule } from 'apollo-angular/testing';
import { firstValueFrom } from 'rxjs';

import { GET_HISTORICO_DE_COTACOES, PrecosService } from './precos.service';

describe('PrecosService', () => {
  function configurar() {
    TestBed.configureTestingModule({ imports: [ApolloTestingModule], providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { service: TestBed.inject(PrecosService), apollo: TestBed.inject(ApolloTestingController), http: TestBed.inject(HttpTestingController) };
  }

  it('deve pedir o histórico do metal com cursor nulo na primeira página', async () => {
    const { service, apollo } = configurar();

    const promessa = firstValueFrom(service.getHistoricoDeCotacoes({ metal: 'OURO', primeiros: 10 }));
    const op = apollo.expectOne(GET_HISTORICO_DE_COTACOES);
    op.flush({ data: { historicoDeCotacoes: { totalCount: 0, pageInfo: { hasNextPage: false, endCursor: null }, nodes: [] } } });
    await promessa;

    expect(op.operation.variables).toEqual({ metal: 'OURO', primeiros: 10, apos: null });
  });

  it('deve escrever cotação, ligas e parâmetros pelo REST', () => {
    const { service, http } = configurar();
    const liga = { nome: 'Ouro 18k', metal: 'OURO' as const, teor: '750', densidade: 15.5 };

    service.postCotacao({ metal: 'PRATA', valor: 7.1 }).subscribe();
    service.putLiga(2, liga).subscribe();
    service.desativarLiga(2).subscribe();
    service.putParametros({ perdaPercentual: 8, valorHora: 90, markup: 1.8, taxasPercentual: 5, arredondarPara: 10 }).subscribe();

    expect(http.expectOne({ method: 'POST', url: 'precificacao/cotacoes' }).request.body).toEqual({ metal: 'PRATA', valor: 7.1 });
    expect(http.expectOne({ method: 'PUT', url: 'precificacao/ligas/2' }).request.body).toEqual(liga);
    expect(http.expectOne({ method: 'POST', url: 'precificacao/ligas/2/desativar' }).request.body).toBeNull();
    expect(http.expectOne({ method: 'PUT', url: 'precificacao/parametros' }).request.body.valorHora).toBe(90);
  });
});

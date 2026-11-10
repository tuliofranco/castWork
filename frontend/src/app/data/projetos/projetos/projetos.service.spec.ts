import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApolloTestingController, ApolloTestingModule } from 'apollo-angular/testing';
import { firstValueFrom } from 'rxjs';

import { GET_PRECOS_DO_PROJETO, GET_PROJETO, ProjetosService } from './projetos.service';

describe('ProjetosService', () => {
  function configurar() {
    TestBed.configureTestingModule({ imports: [ApolloTestingModule], providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { service: TestBed.inject(ProjetosService), apollo: TestBed.inject(ApolloTestingController), http: TestBed.inject(HttpTestingController) };
  }

  it('deve buscar o projeto pelo id e devolver null quando não existe', async () => {
    const { service, apollo } = configurar();
    const promessa = firstValueFrom(service.getProjeto(7));

    const op = apollo.expectOne(GET_PROJETO);
    expect(op.operation.variables).toEqual({ id: 7 });
    op.flush({ data: { projeto: null } });

    expect(await promessa).toBeNull();
  });

  it('deve usar as rotas aninhadas de versão e item', () => {
    const { service, http } = configurar();

    service.postItem(1, 2, { pecaId: 3, quantidade: 12 }).subscribe();
    service.putItem(1, 2, 9, { quantidade: 14 }).subscribe();
    service.deleteItem(1, 2, 9).subscribe();
    service.postPublicarVersao(1, 2).subscribe();
    service.postDuplicarVersao(1, 2).subscribe();

    const chamadas = [
      http.expectOne((r) => r.method === 'POST' && r.url === 'projetos/1/versoes/2/itens'),
      http.expectOne((r) => r.method === 'PUT' && r.url === 'projetos/1/versoes/2/itens/9'),
      http.expectOne((r) => r.method === 'DELETE' && r.url === 'projetos/1/versoes/2/itens/9'),
      http.expectOne((r) => r.method === 'POST' && r.url === 'projetos/1/versoes/2/publicar'),
      http.expectOne((r) => r.method === 'POST' && r.url === 'projetos/1/versoes/2/duplicar'),
    ];
    expect(chamadas[0].request.body).toEqual({ pecaId: 3, quantidade: 12 });
    chamadas.forEach((c) => c.flush(null));
    http.verify();
  });

  it('deve buscar os preços do projeto sempre do servidor', async () => {
    const { service, apollo } = configurar();
    const precos = {
      herdadosEm: '2026-11-09T12:00:00Z',
      alteradosEm: null,
      cotacoes: [{ metal: 'OURO', valor: 640, cotadaEm: '2026-11-08T12:00:00Z' }],
      parametros: { perdaPercentual: 8, valorHora: 90, markup: 1.8, taxasPercentual: 0, arredondarPara: 10 },
    };
    const promessa = firstValueFrom(service.getPrecosDoProjeto(7));

    const op = apollo.expectOne(GET_PRECOS_DO_PROJETO);
    expect(op.operation.variables).toEqual({ id: 7 });
    op.flush({ data: { projeto: { id: 7, precos } } });

    expect(await promessa).toEqual(precos);
  });

  it('deve editar e atualizar os preços pelas rotas do projeto', () => {
    const { service, http } = configurar();
    const dto = { cotacoes: [{ metal: 'OURO' as const, valor: 655 }], perdaPercentual: 8, valorHora: 90, markup: 2, taxasPercentual: 0, arredondarPara: 10 };

    service.putPrecos(1, dto).subscribe();
    service.postAtualizarPrecos(1).subscribe();

    const editar = http.expectOne((r) => r.method === 'PUT' && r.url === 'projetos/1/precos');
    const atualizar = http.expectOne((r) => r.method === 'POST' && r.url === 'projetos/1/precos/atualizar');
    expect(editar.request.body).toEqual(dto);
    [editar, atualizar].forEach((c) => c.flush(null));
    http.verify();
  });
});

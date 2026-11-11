import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApolloTestingController, ApolloTestingModule } from 'apollo-angular/testing';
import { firstValueFrom } from 'rxjs';

import { GET_CONFERENCIA_DO_MODELO, Modelo3dService } from './modelo-3d.service';

describe('Modelo3dService', () => {
  function configurar() {
    TestBed.configureTestingModule({ imports: [ApolloTestingModule], providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { service: TestBed.inject(Modelo3dService), apollo: TestBed.inject(ApolloTestingController), http: TestBed.inject(HttpTestingController) };
  }

  it('deve ler a conferência da versão pelo GraphQL, sempre do servidor', async () => {
    const { service, apollo } = configurar();
    const promessa = firstValueFrom(service.getConferencia(1, 2));

    const op = apollo.expectOne(GET_CONFERENCIA_DO_MODELO);
    expect(op.operation.variables).toEqual({ id: 1, versaoId: 2 });
    op.flush({ data: { projeto: { id: 1, versoes: [{ id: 2, conferenciaDoModelo: { arquivoId: null, medido: false, alteravel: true, pesoDoMetal: 0, pesosPorLiga: [], corpos: [], sobreposicoes: [], materiais: [] } }] } } });

    expect((await promessa)?.arquivoId).toBeNull();
  });

  it('deve definir o material e unir corpos pelas rotas do modelo 3D da versão', () => {
    const { service, http } = configurar();

    service.putMateriais(1, 2, { arquivoId: 9, corpos: [{ corpo: 'Chaton', material: 'Castwork liga 2' }] }).subscribe();
    service.postUnirCorpos(1, 2, { arquivoId: 9, corpos: ['Aro', 'Chaton'] }).subscribe();

    const materiais = http.expectOne((r) => r.method === 'PUT' && r.url === 'projetos/1/versoes/2/modelo-3d/materiais');
    const unir = http.expectOne((r) => r.method === 'POST' && r.url === 'projetos/1/versoes/2/modelo-3d/unir-corpos');
    expect(materiais.request.body).toEqual({ arquivoId: 9, corpos: [{ corpo: 'Chaton', material: 'Castwork liga 2' }] });
    expect(unir.request.body).toEqual({ arquivoId: 9, corpos: ['Aro', 'Chaton'] });
    materiais.flush({ arquivoId: 10, arquivoAnteriorId: 9 });
    unir.flush({ arquivoId: 11, arquivoAnteriorId: 10 });
    http.verify();
  });

  it('deve montar o modelo da versão a partir das peças pela rota do modelo 3D', () => {
    const { service, http } = configurar();

    service.postMontarAPartirDasPecas(1, 2).subscribe();

    const montar = http.expectOne((r) => r.method === 'POST' && r.url === 'projetos/1/versoes/2/modelo-3d/montar-a-partir-das-pecas');
    montar.flush({ arquivoId: 12, arquivoAnteriorId: null, pecas: [], pecasSemModelo: [] });
    expect(() => http.verify()).not.toThrow();
  });
});

import { TestBed } from '@angular/core/testing';
import { gql } from '@apollo/client';
import { Apollo } from 'apollo-angular';
import { ApolloTestingController, ApolloTestingModule } from 'apollo-angular/testing';
import { GraphQLError } from 'graphql';
import { firstValueFrom } from 'rxjs';

import { RespostaGqlSemDadosError, dadosGql } from './dados-gql.operator';

const PECAS = gql`
  query Pecas {
    pecas {
      id
      nome
    }
  }
`;

interface PecasQueryGql {
  pecas: { id: string; nome: string }[];
}

describe('dadosGql (com ApolloTestingModule)', () => {
  let apollo: Apollo;
  let controller: ApolloTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ApolloTestingModule] });
    apollo = TestBed.inject(Apollo);
    controller = TestBed.inject(ApolloTestingController);
  });

  afterEach(() => controller.verify());

  const consultar = () => firstValueFrom(apollo.query<PecasQueryGql>({ query: PECAS, errorPolicy: 'all' }).pipe(dadosGql()));

  it('deve emitir data quando a resposta vem só com data', async () => {
    const promessa = consultar();
    controller.expectOne(PECAS).flush({ data: { pecas: [{ __typename: 'Peca', id: '1', nome: 'Anel' }] } });
    expect((await promessa).pecas[0]?.nome).toBe('Anel');
  });

  it('deve emitir data parcial quando vêm data E errors', async () => {
    const promessa = consultar();
    controller.expectOne(PECAS).flush({
      data: { pecas: [{ __typename: 'Peca', id: '1', nome: 'Anel' }] },
      errors: [new GraphQLError('campo cliente falhou')],
    });
    expect((await promessa).pecas).toHaveLength(1);
  });

  it('deve lançar quando vêm só errors', async () => {
    const promessa = consultar();
    controller.expectOne(PECAS).flush({ errors: [new GraphQLError('falhou tudo')] });
    await expect(promessa).rejects.toBeInstanceOf(RespostaGqlSemDadosError);
  });
});

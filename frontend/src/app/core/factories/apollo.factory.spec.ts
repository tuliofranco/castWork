import { HttpContext } from '@angular/common/http';
import { ApolloClient, ApolloLink, type TypedDocumentNode, gql } from '@apollo/client';
import { BYPASS_ERROR } from '@shared/injection-tokens/http-context.tokens';
import type { ToastService } from '@shared/services/toast.service';
import { of } from 'rxjs';

import { OPCOES_PADRAO, criarCache, criarLinkDeErro } from './apollo.factory';

const PECA = gql`
  query Peca($id: ID!) {
    peca(id: $id) {
      id
      nome
    }
  }
`;

const PECAS: TypedDocumentNode<{ pecas: { __typename: 'Peca'; id: string; nome: string }[] }> = gql`
  query Pecas {
    pecas {
      id
      nome
    }
  }
`;

describe('Apollo: cache normalizado', () => {
  it('deve normalizar por __typename:id', () => {
    const cache = criarCache();
    cache.writeQuery({ query: PECAS, data: { pecas: [{ __typename: 'Peca', id: '1', nome: 'Anel' }] } });
    expect(cache.extract()['Peca:1']).toEqual({ __typename: 'Peca', id: '1', nome: 'Anel' });
  });

  it('cache redirect: peca(id) deve ler a peça que veio na listagem', () => {
    const cache = criarCache();
    cache.writeQuery({ query: PECAS, data: { pecas: [{ __typename: 'Peca', id: '1', nome: 'Anel' }] } });
    expect(cache.readQuery({ query: PECA, variables: { id: '1' } })).toEqual({
      peca: { __typename: 'Peca', id: '1', nome: 'Anel' },
    });
  });

  it('atualizar a entidade deve refletir em todas as queries que a referenciam', () => {
    const cache = criarCache();
    cache.writeQuery({ query: PECAS, data: { pecas: [{ __typename: 'Peca', id: '1', nome: 'Anel' }] } });
    cache.writeQuery({ query: PECA, variables: { id: '1' }, data: { peca: { __typename: 'Peca', id: '1', nome: 'Anel solitário' } } });
    expect(cache.readQuery({ query: PECAS })?.pecas[0]?.nome).toBe('Anel solitário');
  });
});

describe('Apollo: resposta com data e errors ao mesmo tempo', () => {
  const RESPOSTA_PARCIAL = {
    data: { pecas: [{ __typename: 'Peca', id: '1', nome: 'Anel' }] },
    errors: [{ message: 'Falha ao carregar o cliente da peça', path: ['pecas', 0, 'cliente'] }],
  };

  function criarCliente(toast: Pick<ToastService, 'erro'>) {
    const servidorFalso = new ApolloLink(() => of(RESPOSTA_PARCIAL));
    return new ApolloClient({
      link: ApolloLink.from([criarLinkDeErro(toast as ToastService), servidorFalso]),
      cache: criarCache(),
      defaultOptions: OPCOES_PADRAO,
    });
  }

  it('com errorPolicy all: entrega data E error, e mostra o toast', async () => {
    const toast = { erro: vi.fn() };
    const resultado = await criarCliente(toast).query({ query: PECAS });
    expect(resultado.data?.pecas).toHaveLength(1);
    expect(resultado.error?.message).toContain('Falha ao carregar o cliente');
    expect(toast.erro).toHaveBeenCalledWith('Falha ao carregar o cliente da peça');
  });

  it('BYPASS_ERROR no context deve suprimir o toast', async () => {
    const toast = { erro: vi.fn() };
    await criarCliente(toast).query({
      query: PECAS,
      context: { httpContext: new HttpContext().set(BYPASS_ERROR, true) },
    });
    expect(toast.erro).not.toHaveBeenCalled();
  });

  it('erro de rede não deve gerar toast no link (o interceptor HTTP já mostrou)', async () => {
    const toast = { erro: vi.fn() };
    const client = new ApolloClient({
      link: ApolloLink.from([criarLinkDeErro(toast as unknown as ToastService), new ApolloLink(() => {
        throw new Error('rede');
      })]),
      cache: criarCache(),
      defaultOptions: OPCOES_PADRAO,
    });
    await client.query({ query: PECAS }).catch(() => undefined);
    expect(toast.erro).not.toHaveBeenCalled();
  });
});

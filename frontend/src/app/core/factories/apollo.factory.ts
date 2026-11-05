import { HttpContext } from '@angular/common/http';
import { inject } from '@angular/core';
import { ApolloClient, ApolloLink, InMemoryCache } from '@apollo/client';
import { CombinedGraphQLErrors } from '@apollo/client/errors';
import { ErrorLink } from '@apollo/client/link/error';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { BYPASS_ERROR } from '@shared/injection-tokens/http-context.tokens';
import { ToastService } from '@shared/services/toast.service';
import { HttpLink } from 'apollo-angular/http';

/**
 * Cache normalizado: o Apollo quebra cada resposta em objetos guardados por `__typename:id`
 * (ex.: `Peca:42`). Duas queries que trazem a mesma peça apontam para a MESMA entrada,
 * então atualizar uma atualiza todas. `typePolicies` ensina o cache a lidar com casos especiais.
 */
export function criarCache(): InMemoryCache {
  return new InMemoryCache({
    typePolicies: {
      Query: {
        fields: {
          /**
           * Cache redirect: `peca(id: 42)` nunca foi pedida, mas `Peca:42` já está no cache
           * (veio na listagem). O `read` devolve a referência e a tela de detalhes abre sem
           * ida à rede — quando a fetchPolicy permite ler do cache.
           */
          peca: {
            read: (existente: unknown, { args, toReference }) =>
              existente ?? (typeof args?.['id'] === 'string' ? toReference({ __typename: 'Peca', id: args['id'] }) : undefined),
          },
        },
      },
      Peca: {
        // Sem keyFields: o padrão usa `id` e gera a chave `Peca:42`. Declarar `keyFields: ['id']`
        // parece equivalente, mas muda a chave para `Peca:{"id":"42"}`. Use keyFields só para
        // chave diferente de id (ex.: ['codigo']) ou composta (ex.: ['atelieId', 'numero']).
        fields: {
          /**
           * Lista sem id próprio: por padrão o Apollo avisa que vai sobrescrever e não sabe mesclar.
           * `merge: false` diz "a lista nova substitui a antiga" — o certo para etapas da peça.
           */
          etapas: { merge: false },
        },
      },
      /** Valor embutido (sem id): fica dentro do objeto pai em vez de virar entrada própria. */
      ComposicaoPreco: { keyFields: false },
    },
  });
}

/** Lê o HttpContext que a chamada passou em `context: { httpContext }`. */
function contextoHttp(operation: ApolloLink.Operation): HttpContext | undefined {
  const { httpContext } = operation.getContext();
  return httpContext;
}

/**
 * Erros de GraphQL chegam com HTTP 200, então o erroGlobalInterceptor não os vê.
 * Este link mostra o toast deles. Erros de rede NÃO são tratados aqui: passaram pelo
 * HttpClient e o interceptor já mostrou o toast (evita toast duplicado).
 */
export function criarLinkDeErro(toast: ToastService): ErrorLink {
  return new ErrorLink(({ error, operation }) => {
    if (!CombinedGraphQLErrors.is(error) || contextoHttp(operation)?.get(BYPASS_ERROR)) return;
    for (const { message } of error.errors) toast.erro(message);
  });
}

/**
 * fetchPolicy (de onde vêm os dados):
 * - cache-first: usa o cache se tiver tudo; senão rede. Padrão do Apollo; risco de dado velho.
 * - cache-and-network: entrega o cache na hora e atualiza com a rede (só em watchQuery).
 * - network-only: sempre rede, mas grava no cache (outras telas aproveitam).
 * - no-cache: sempre rede e não grava. - cache-only: nunca vai à rede.
 *
 * errorPolicy (o que fazer com `errors`):
 * - none: qualquer erro descarta `data` (padrão). - ignore: descarta `errors`, entrega `data`.
 * - all: entrega `data` E `error` juntos — necessário para resposta parcial.
 *
 * Escolhas: `network-only` porque a regra do projeto é "a API é a fonte da verdade" (sem dado fantasma);
 * `all` porque HotChocolate devolve `data` parcial + `errors` quando um campo falha.
 */
export const OPCOES_PADRAO = {
  watchQuery: { fetchPolicy: 'cache-and-network', errorPolicy: 'all' },
  query: { fetchPolicy: 'network-only', errorPolicy: 'all' },
  mutate: { errorPolicy: 'all' },
} as const satisfies ApolloClient.DefaultOptions.Input;

export function apolloFactory(): ApolloClient.Options {
  const httpLink = inject(HttpLink);
  const ambiente = inject(AMBIENTE);
  const toast = inject(ToastService);

  return {
    // HttpLink usa o HttpClient: as requisições GraphQL passam pelos mesmos interceptors (JWT, loading, erro).
    link: ApolloLink.from([criarLinkDeErro(toast), httpLink.create({ uri: ambiente.graphqlUrl })]),
    cache: criarCache(),
    defaultOptions: OPCOES_PADRAO,
  };
}

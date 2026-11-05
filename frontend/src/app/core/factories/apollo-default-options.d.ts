import '@apollo/client';

/**
 * Apollo Client 4: a errorPolicy padrão precisa ser declarada também nos TIPOS.
 * Com 'all', o TypeScript passa a tipar `data` como possivelmente parcial/undefined e `error` como
 * presente junto — exatamente o que o operador `dadosGql` trata. Deve casar com `apolloFactory`.
 */
declare module '@apollo/client' {
  export namespace ApolloClient {
    export namespace DeclareDefaultOptions {
      interface WatchQuery {
        errorPolicy: 'all';
      }
      interface Query {
        errorPolicy: 'all';
      }
      interface Mutate {
        errorPolicy: 'all';
      }
    }
  }
}

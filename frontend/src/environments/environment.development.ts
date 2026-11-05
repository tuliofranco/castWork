import type { Environment } from './environment.model';

/**
 * Desenvolvimento contra o backend .NET local. As URLs são relativas, como em produção:
 * o `ng serve` repassa /api e /graphql para https://localhost:5001 (proxy.conf.json),
 * então não há CORS nem certificado no navegador. Para trabalhar sem o backend, mude usarMocks para true.
 */
export const environment: Environment = {
  production: false,
  apiUrl: '/api',
  graphqlUrl: '/graphql',
  usarMocks: false,
};

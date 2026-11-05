import type { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  graphqlUrl: 'https://localhost:5001/graphql',
  usarMocks: true,
};

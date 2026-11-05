/** Contrato único de configuração por ambiente. Todos os environments devem satisfazê-lo. */
export interface Environment {
  readonly production: boolean;
  /** URL base dos endpoints REST (ex.: download de PDF). */
  readonly apiUrl: string;
  /** Endpoint GraphQL (HotChocolate). */
  readonly graphqlUrl: string;
  /** Quando true, usa mocks locais no lugar do backend .NET. */
  readonly usarMocks: boolean;
}

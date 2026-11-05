/** Página de uma connection GraphQL (paginação por cursor do HotChocolate). */
export interface PaginaDto<T> {
  readonly totalCount: number;
  readonly pageInfo: { readonly hasNextPage: boolean; readonly endCursor: string | null };
  readonly nodes: readonly T[];
}

/** Variáveis comuns das listagens: busca livre e cursor da próxima página. */
export interface ConsultaPaginadaDto {
  readonly termo?: string | null;
  readonly primeiros?: number;
  readonly apos?: string | null;
}

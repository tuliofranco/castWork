import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { ConsultaPaginadaDto, PaginaDto } from '@data/common/graphql/pagina.dto';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { PecaDto } from './dto/peca.dto';
import type { PostPecaDto } from './dto/post-peca.dto';

export const GET_PECAS = gql`
  query GetPecas($termo: String, $primeiros: Int, $apos: String, $filtro: PecaFiltro) {
    pecas(termo: $termo, first: $primeiros, after: $apos, where: $filtro) {
      totalCount
      pageInfo {
        hasNextPage
        endCursor
      }
      nodes {
        id
        nome
        tipo
        material
        teor
        pesoGramas
        ativo
      }
    }
  }
`;

interface GetPecasResposta {
  readonly pecas: PaginaDto<PecaDto>;
}

export interface ConsultaPecasDto extends ConsultaPaginadaDto {
  /** true para o seletor da composição (peça inativa não entra em versão nova). */
  readonly somenteAtivas?: boolean | null;
}

/** Biblioteca de peças: leitura pelo GraphQL, cadastro pelo REST. */
@Injectable({ providedIn: 'root' })
export class PecasService {
  private readonly apollo = inject(Apollo);
  private readonly http = inject(HttpClient);

  getPecas(consulta: ConsultaPecasDto): Observable<PaginaDto<PecaDto>> {
    return this.apollo
      .query<GetPecasResposta>({
        query: GET_PECAS,
        // Filtro nulo = sem filtro. (`ativo: { eq: null }` compararia com nulo e não traria nada.)
        variables: {
          termo: consulta.termo ?? null,
          primeiros: consulta.primeiros,
          apos: consulta.apos ?? null,
          filtro: consulta.somenteAtivas ? { ativo: { eq: true } } : null,
        },
      })
      .pipe(
        dadosGql(),
        map((dados) => dados.pecas),
      );
  }

  postPeca(dto: PostPecaDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>('pecas', dto);
  }
}

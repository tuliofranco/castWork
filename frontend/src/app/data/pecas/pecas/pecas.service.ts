import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { ConsultaPaginadaDto, PaginaDto } from '@data/common/graphql/pagina.dto';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { PecaResumoDto } from './dto/peca-resumo.dto';
import type { PostPecaDto } from './dto/post-peca.dto';

export const GET_PECAS = gql`
  query GetPecas($termo: String, $primeiros: Int, $apos: String) {
    pecas(termo: $termo, first: $primeiros, after: $apos) {
      totalCount
      pageInfo {
        hasNextPage
        endCursor
      }
      nodes {
        id
        titulo
        status
        prazoEntrega
        criadoEm
        cliente {
          id
          nome
        }
      }
    }
  }
`;

interface GetPecasResposta {
  readonly pecas: PaginaDto<PecaResumoDto>;
}

/** Leitura pelo GraphQL (telas), escrita pelo REST (commands). */
@Injectable({ providedIn: 'root' })
export class PecasService {
  private readonly apollo = inject(Apollo);
  private readonly http = inject(HttpClient);

  getPecas(consulta: ConsultaPaginadaDto): Observable<PaginaDto<PecaResumoDto>> {
    return this.apollo
      .query<GetPecasResposta>({ query: GET_PECAS, variables: { ...consulta } })
      .pipe(
        dadosGql(),
        map((dados) => dados.pecas),
      );
  }

  postPeca(dto: PostPecaDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>('pecas', dto);
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { ConsultaPaginadaDto, PaginaDto } from '@data/common/graphql/pagina.dto';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { PecaDetalheDto, PecaDto } from './dto/peca.dto';
import type { PostPecaDto } from './dto/post-peca.dto';
import type { PutPecaDto } from './dto/put-peca.dto';

export const GET_PECAS = gql`
  query GetPecas(
    $termo: String
    $primeiros: Int
    $apos: String
    $filtro: PecaFiltro
    $comCusto: Boolean! = false
  ) {
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
        custo @include(if: $comCusto)
      }
    }
  }
`;

/** O custo só é pedido por quem tem `precificacao.ler`: para os demais o campo responde erro de autorização. */
export const GET_PECA = gql`
  query GetPeca($id: Long!, $comCusto: Boolean! = false) {
    peca(id: $id) {
      id
      nome
      tipo
      material
      teor
      pesoGramas
      descricao
      ativo
      custo @include(if: $comCusto)
    }
  }
`;

interface GetPecaResposta {
  readonly peca: PecaDetalheDto | null;
}

interface GetPecasResposta {
  readonly pecas: PaginaDto<PecaDto>;
}

export interface ConsultaPecasDto extends ConsultaPaginadaDto {
  /** true para o seletor da composição (peça inativa não entra em versão nova). */
  readonly somenteAtivas?: boolean | null;
  /** true só para quem tem `precificacao.ler`. */
  readonly comCusto?: boolean | null;
}

/** Biblioteca de peças: leitura pelo GraphQL, cadastro e alteração pelo REST. */
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
          comCusto: consulta.comCusto ?? false,
        },
      })
      .pipe(
        dadosGql(),
        map((dados) => dados.pecas),
      );
  }

  getPeca(id: number, comCusto = false): Observable<PecaDetalheDto | null> {
    return this.apollo
      .query<GetPecaResposta>({ query: GET_PECA, variables: { id, comCusto } })
      .pipe(
        dadosGql(),
        map((dados) => dados.peca),
      );
  }

  postPeca(dto: PostPecaDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>('pecas', dto);
  }

  putPeca(id: number, dto: PutPecaDto): Observable<void> {
    return this.http.put(`pecas/${id}`, dto).pipe(map(() => undefined));
  }
}

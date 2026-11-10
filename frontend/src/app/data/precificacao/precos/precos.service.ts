import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { PaginaDto } from '@data/common/graphql/pagina.dto';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type {
  CotacaoDto,
  GetHistoricoDeCotacoesQuery,
  PostCotacaoDto,
  PostLigaDto,
  PrecosDoAtelieDto,
  PutLigaDto,
  PutParametrosDePrecoDto,
} from './dto/precos.dto';

export const GET_PRECOS_DO_ATELIE = gql`
  query GetPrecosDoAtelie {
    precosDoAtelie {
      cotacoes {
        metal
        atual {
          valor
          registradaEm
          registradaPor
        }
      }
      ligas {
        id
        nome
        metal
        teor
        densidade
      }
      parametros {
        perdaPercentual
        valorHora
        markup
        taxasPercentual
        arredondarPara
      }
    }
  }
`;

export const GET_HISTORICO_DE_COTACOES = gql`
  query GetHistoricoDeCotacoes($metal: Metal!, $primeiros: Int, $apos: String) {
    historicoDeCotacoes(metal: $metal, first: $primeiros, after: $apos) {
      totalCount
      pageInfo {
        hasNextPage
        endCursor
      }
      nodes {
        valor
        registradaEm
        registradaPor
      }
    }
  }
`;

interface GetPrecosDoAtelieResposta {
  readonly precosDoAtelie: PrecosDoAtelieDto;
}

interface GetHistoricoDeCotacoesResposta {
  readonly historicoDeCotacoes: PaginaDto<CotacaoDto>;
}

/** Preços do ateliê (cotações, ligas e parâmetros): leitura pelo GraphQL, escrita pelo REST. */
@Injectable({ providedIn: 'root' })
export class PrecosService {
  private readonly apollo = inject(Apollo);
  private readonly http = inject(HttpClient);

  getPrecos(): Observable<PrecosDoAtelieDto> {
    // Sempre do servidor: a tela salva e relê, e o cache do Apollo mostraria o valor antigo.
    return this.apollo.query<GetPrecosDoAtelieResposta>({ query: GET_PRECOS_DO_ATELIE, fetchPolicy: 'network-only' }).pipe(
      dadosGql(),
      map((dados) => dados.precosDoAtelie),
    );
  }

  getHistoricoDeCotacoes(consulta: GetHistoricoDeCotacoesQuery): Observable<PaginaDto<CotacaoDto>> {
    return this.apollo
      .query<GetHistoricoDeCotacoesResposta>({
        query: GET_HISTORICO_DE_COTACOES,
        variables: { metal: consulta.metal, primeiros: consulta.primeiros, apos: consulta.apos ?? null },
        fetchPolicy: 'network-only',
      })
      .pipe(
        dadosGql(),
        map((dados) => dados.historicoDeCotacoes),
      );
  }

  postCotacao(dto: PostCotacaoDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>('precificacao/cotacoes', dto);
  }

  postLiga(dto: PostLigaDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>('precificacao/ligas', dto);
  }

  putLiga(id: number, dto: PutLigaDto): Observable<void> {
    return this.http.put(`precificacao/ligas/${id}`, dto).pipe(map(() => undefined));
  }

  desativarLiga(id: number): Observable<void> {
    return this.http.post(`precificacao/ligas/${id}/desativar`, null).pipe(map(() => undefined));
  }

  putParametros(dto: PutParametrosDePrecoDto): Observable<void> {
    return this.http.put('precificacao/parametros', dto).pipe(map(() => undefined));
  }
}

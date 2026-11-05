import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { ConsultaPaginadaDto, PaginaDto } from '@data/common/graphql/pagina.dto';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { PostItemDto, PostProjetoDto, PostVersaoDto, PutItemDto } from './dto/projeto-comandos.dto';
import type { ProjetoDto, ProjetoResumoDto } from './dto/projeto.dto';

export const GET_PROJETOS = gql`
  query GetProjetos($termo: String, $primeiros: Int, $apos: String) {
    projetos(termo: $termo, first: $primeiros, after: $apos) {
      totalCount
      pageInfo {
        hasNextPage
        endCursor
      }
      nodes {
        id
        nome
        cliente {
          id
          nome
        }
        versoes {
          id
          numero
          status
        }
      }
    }
  }
`;

export const GET_PROJETO = gql`
  query GetProjeto($id: Long!) {
    projeto(id: $id) {
      id
      nome
      descricao
      cliente {
        id
        nome
      }
      versoes {
        id
        numero
        nome
        observacoes
        status
        publicadaEm
        itens {
          id
          quantidade
          observacao
          peca {
            id
            nome
            tipo
            material
            teor
            pesoGramas
          }
        }
      }
    }
  }
`;

interface GetProjetosResposta {
  readonly projetos: PaginaDto<ProjetoResumoDto>;
}

interface GetProjetoResposta {
  readonly projeto: ProjetoDto | null;
}

/**
 * Projetos, versões e composição. Leitura pelo GraphQL; cada alteração é um command REST
 * (rotas aninhadas: /projetos/{id}/versoes/{versaoId}/itens/{itemId}).
 */
@Injectable({ providedIn: 'root' })
export class ProjetosService {
  private readonly apollo = inject(Apollo);
  private readonly http = inject(HttpClient);

  getProjetos(consulta: ConsultaPaginadaDto): Observable<PaginaDto<ProjetoResumoDto>> {
    return this.apollo
      .query<GetProjetosResposta>({ query: GET_PROJETOS, variables: { ...consulta } })
      .pipe(
        dadosGql(),
        map((dados) => dados.projetos),
      );
  }

  getProjeto(id: number): Observable<ProjetoDto | null> {
    return this.apollo.query<GetProjetoResposta>({ query: GET_PROJETO, variables: { id } }).pipe(
      dadosGql(),
      map((dados) => dados.projeto),
    );
  }

  postProjeto(dto: PostProjetoDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>('projetos', dto);
  }

  postVersao(projetoId: number, dto: PostVersaoDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>(`projetos/${projetoId}/versoes`, dto);
  }

  postDuplicarVersao(projetoId: number, versaoId: number): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>(`projetos/${projetoId}/versoes/${versaoId}/duplicar`, {});
  }

  putCliente(projetoId: number, clienteId: number | null): Observable<void> {
    return this.http.put(`projetos/${projetoId}/cliente`, { clienteId }).pipe(map(() => undefined));
  }

  postArquivarVersao(projetoId: number, versaoId: number): Observable<void> {
    return this.http.post(`projetos/${projetoId}/versoes/${versaoId}/arquivar`, {}).pipe(map(() => undefined));
  }

  postDesarquivarVersao(projetoId: number, versaoId: number): Observable<void> {
    return this.http.post(`projetos/${projetoId}/versoes/${versaoId}/desarquivar`, {}).pipe(map(() => undefined));
  }

  postPublicarVersao(projetoId: number, versaoId: number): Observable<void> {
    return this.http.post(`projetos/${projetoId}/versoes/${versaoId}/publicar`, {}).pipe(map(() => undefined));
  }

  postItem(projetoId: number, versaoId: number, dto: PostItemDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>(`projetos/${projetoId}/versoes/${versaoId}/itens`, dto);
  }

  putItem(projetoId: number, versaoId: number, itemId: number, dto: PutItemDto): Observable<void> {
    return this.http.put(`projetos/${projetoId}/versoes/${versaoId}/itens/${itemId}`, dto).pipe(map(() => undefined));
  }

  deleteItem(projetoId: number, versaoId: number, itemId: number): Observable<void> {
    return this.http.delete(`projetos/${projetoId}/versoes/${versaoId}/itens/${itemId}`).pipe(map(() => undefined));
  }
}

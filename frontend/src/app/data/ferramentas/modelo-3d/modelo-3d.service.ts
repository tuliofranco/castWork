import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { ConferenciaDoModeloDto, ModeloAlteradoDto, PostUnirCorposDto, PutMateriaisDosCorposDto } from './dto/conferencia-do-modelo.dto';
import type { ModeloMontadoDto } from './dto/modelo-montado.dto';

/** Conferência de peso e materiais: mede o arquivo 3D atual da versão (a medição fica guardada pelo arquivo). */
export const GET_CONFERENCIA_DO_MODELO = gql`
  query GetConferenciaDoModelo($id: Long!, $versaoId: Long!) {
    projeto(id: $id) {
      id
      versoes(id: $versaoId) {
        id
        conferenciaDoModelo {
          arquivoId
          medido
          alteravel
          pesoDoMetal
          pesosPorLiga {
            ligaId
            liga
            peso
          }
          corpos {
            nome
            material
            uso
            ligaId
            liga
            volumeMm3
            massaGramas
            fechado
            contaNoPeso
          }
          sobreposicoes {
            corpoA
            corpoB
            volumeMm3
            deMetal
            podeUnir
          }
          materiais {
            nome
            rotulo
            uso
            ligaId
          }
        }
      }
    }
  }
`;

interface GetConferenciaDoModeloResposta {
  readonly projeto: { readonly id: number; readonly versoes: readonly { readonly id: number; readonly conferenciaDoModelo: ConferenciaDoModeloDto }[] } | null;
}

/**
 * Modelo 3D da versão fora da conversa com a IA: a conferência de peso e materiais e o "Montar a partir das peças".
 * Leitura pelo GraphQL; definir o material, unir corpos e montar são commands REST
 * (`/projetos/{id}/versoes/{versaoId}/modelo-3d/...`), e cada um grava uma nova versão do arquivo.
 */
@Injectable({ providedIn: 'root' })
export class Modelo3dService {
  private readonly apollo = inject(Apollo);
  private readonly http = inject(HttpClient);

  getConferencia(projetoId: number, versaoId: number): Observable<ConferenciaDoModeloDto | null> {
    // Sempre do servidor: o arquivo muda a cada alteração (da IA, do joalheiro ou da própria conferência).
    return this.apollo
      .query<GetConferenciaDoModeloResposta>({ query: GET_CONFERENCIA_DO_MODELO, variables: { id: projetoId, versaoId }, fetchPolicy: 'network-only' })
      .pipe(
        dadosGql(),
        map((dados) => dados.projeto?.versoes[0]?.conferenciaDoModelo ?? null),
      );
  }

  putMateriais(projetoId: number, versaoId: number, dto: PutMateriaisDosCorposDto): Observable<ModeloAlteradoDto> {
    return this.http.put<ModeloAlteradoDto>(`projetos/${projetoId}/versoes/${versaoId}/modelo-3d/materiais`, dto);
  }

  postUnirCorpos(projetoId: number, versaoId: number, dto: PostUnirCorposDto): Observable<ModeloAlteradoDto> {
    return this.http.post<ModeloAlteradoDto>(`projetos/${projetoId}/versoes/${versaoId}/modelo-3d/unir-corpos`, dto);
  }

  /** Uma cópia do modelo de cada peça da composição por unidade, enfileiradas no modelo da versão. */
  postMontarAPartirDasPecas(projetoId: number, versaoId: number): Observable<ModeloMontadoDto> {
    return this.http.post<ModeloMontadoDto>(`projetos/${projetoId}/versoes/${versaoId}/modelo-3d/montar-a-partir-das-pecas`, null);
  }
}

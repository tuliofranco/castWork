import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { ImagemGuardadaDto, VistaDaImagem } from '@data/assistente/dto/ferramenta.dto';
import type { ConsultaPaginadaDto, PaginaDto } from '@data/common/graphql/pagina.dto';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { PecaDetalheDto, PecaDto } from './dto/peca.dto';

export const GET_PECAS = gql`
  query GetPecas(
    $termo: String
    $primeiros: Int
    $apos: String
    $filtro: PecaFiltro
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
      }
    }
  }
`;

/**
 * Detalhe de consulta. O histórico de uso exige `projetos.ler` e o valor em cada projeto, `precificacao.ler`: sem a
 * regra, o campo responde erro de autorização, então só é pedido por quem a tem.
 */
export const GET_PECA = gql`
  query GetPeca($id: Long!, $comHistorico: Boolean!, $comValor: Boolean!) {
    peca(id: $id) {
      id
      nome
      tipo
      material
      teor
      pesoGramas
      descricao
      ativo
      desenhos {
        ferramenta
        copiadoEm
        tamanho
      }
      historicoDeUso @include(if: $comHistorico) {
        projetoId
        projeto
        pecaDoProjetoId
        valor @include(if: $comValor)
        versoes {
          versaoId
          numero
          nome
          status
          quantidade
        }
      }
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
  /** true para o "Importar da biblioteca" do projeto (peça inativa não é importada). */
  readonly somenteAtivas?: boolean | null;
}

export interface ConsultaPecaDto {
  /** true só para quem tem `projetos.ler`. */
  readonly comHistorico: boolean;
  /** true só para quem tem `precificacao.ler` (o valor de cada projeto no histórico). */
  readonly comValor: boolean;
}

/** Biblioteca de peças: só leitura pela tela (a biblioteca é povoada pela publicação das peças do projeto). */
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

  getPeca(id: number, consulta: ConsultaPecaDto): Observable<PecaDetalheDto | null> {
    return this.apollo
      .query<GetPecaResposta>({ query: GET_PECA, variables: { id, ...consulta } })
      .pipe(
        dadosGql(),
        map((dados) => dados.peca),
      );
  }

  /** O desenho da peça numa ferramenta (DXF do `desenho-2d`, JSON do `modelo-3d`), só para ver; nulo se não tem (204). */
  getDesenho(id: number, ferramenta: string): Observable<Blob | null> {
    return this.http
      .get(`pecas/${id}/desenhos/${ferramenta}`, { observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : r.body)));
  }

  /** Prévia (SVG) do desenho da peça da biblioteca; nulo se ela não tem esse desenho. */
  getPreviaDoDesenho(id: number, ferramenta: string): Observable<Blob | null> {
    return this.http
      .get(`pecas/${id}/desenhos/${ferramenta}/previa`, { observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : r.body)));
  }

  /** As imagens guardadas do desenho da peça da biblioteca (vista e tipo); vazia se foi publicada antes de existirem. */
  getImagensDoDesenho(id: number, ferramenta: string): Observable<readonly ImagemGuardadaDto[]> {
    return this.http.get<readonly ImagemGuardadaDto[]>(`pecas/${id}/desenhos/${ferramenta}/imagens`);
  }

  /** Uma imagem guardada do desenho da peça da biblioteca; sem `formato`, a de melhor qualidade. Nula se não há (204). */
  getImagemDoDesenho(id: number, ferramenta: string, vista: VistaDaImagem, formato?: 'png' | 'svg'): Observable<Blob | null> {
    const params: Record<string, string> = formato ? { formato } : {};
    return this.http
      .get(`pecas/${id}/desenhos/${ferramenta}/imagens/${vista}`, { params, observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : r.body)));
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { ConsultaPaginadaDto, PaginaDto } from '@data/common/graphql/pagina.dto';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { PrecoDaVersaoDto, PutMaoDeObraDto, PutMarkupDaVersaoDto, PutMetaisDaVersaoDto, PutPesoRealDto, PutServicosDto, TipoMaoDeObra } from './dto/preco-da-versao.dto';
import type { PostItemDto, PostProjetoDto, PostVersaoDto, PutItemDto, PutPrecosDoProjetoDto } from './dto/projeto-comandos.dto';
import type { PrecosDoProjetoDto, ProjetoDto, ProjetoResumoDto } from './dto/projeto.dto';

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

/** Separada do GET_PROJETO: cotações e parâmetros exigem `precificacao.ler` e só são pedidos por quem tem a regra. */
export const GET_PRECOS_DO_PROJETO = gql`
  query GetPrecosDoProjeto($id: Long!) {
    projeto(id: $id) {
      id
      precos {
        herdadosEm
        alteradosEm
        cotacoes {
          metal
          valor
          cotadaEm
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
  }
`;

/**
 * Preço de uma versão, calculado a cada leitura. O `calculo` (custos, markup, cotações, pendências) exige
 * `precificacao.ler`: sem a regra, `comCalculo` é false e o campo nem é pedido.
 */
export const GET_PRECO_DA_VERSAO = gql`
  query GetPrecoDaVersao($id: Long!, $versaoId: Long!, $comCalculo: Boolean!) {
    projeto(id: $id) {
      id
      versoes(id: $versaoId) {
        id
        preco {
          preco
          status
          calculo @include(if: $comCalculo) {
            origemDoPeso
            pesosManuaisGuardados {
              ligaId
              liga
              peso
            }
            metais {
              ligaId
              liga
              metal
              teor
              ligaAtiva
              pesoEstimado
              pesoAFundir
              cotacaoDoPuro
              valorPorGrama
              custo
              pesoReal
              diferencaDoPesoReal
            }
            custoDosMetais
            composicao {
              pecaId
              nome
              quantidade
              custoUnitario
              custo
            }
            custoDaComposicao
            maoDeObra {
              tipo
              horas
              valorHora
              custo
            }
            servicos {
              nome
              valor
            }
            custoDosServicos
            custoTotal
            perdaPercentual
            markup
            markupDaVersao
            taxasPercentual
            arredondarPara
            precoSemArredondar
            pendencias {
              tipo
              descricao
              pecaId
              ligaId
              metal
              corpos
            }
            avisos {
              tipo
              descricao
              corpos
            }
          }
        }
      }
    }
  }
`;

/** O REST lê o enum pelo nome do C# (PorHoras), não pelo do GraphQL (POR_HORAS). */
const TIPO_MAO_DE_OBRA_REST: Readonly<Record<TipoMaoDeObra, string>> = { POR_HORAS: 'PorHoras', VALOR_FIXO: 'ValorFixo' };

interface GetProjetosResposta {
  readonly projetos: PaginaDto<ProjetoResumoDto>;
}

interface GetProjetoResposta {
  readonly projeto: ProjetoDto | null;
}

interface GetPrecoDaVersaoResposta {
  readonly projeto: { readonly id: number; readonly versoes: readonly { readonly id: number; readonly preco: PrecoDaVersaoDto }[] } | null;
}

interface GetPrecosDoProjetoResposta {
  readonly projeto: { readonly id: number; readonly precos: PrecosDoProjetoDto } | null;
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

  getPrecosDoProjeto(id: number): Observable<PrecosDoProjetoDto | null> {
    // Sempre do servidor: a tela edita e relê, e o cache do Apollo mostraria o valor antigo.
    return this.apollo
      .query<GetPrecosDoProjetoResposta>({ query: GET_PRECOS_DO_PROJETO, variables: { id }, fetchPolicy: 'network-only' })
      .pipe(
        dadosGql(),
        map((dados) => dados.projeto?.precos ?? null),
      );
  }

  putPrecos(projetoId: number, dto: PutPrecosDoProjetoDto): Observable<void> {
    return this.http.put(`projetos/${projetoId}/precos`, dto).pipe(map(() => undefined));
  }

  /** "Atualizar para os valores atuais": cotações e parâmetros de hoje do ateliê. */
  postAtualizarPrecos(projetoId: number): Observable<void> {
    return this.http.post(`projetos/${projetoId}/precos/atualizar`, {}).pipe(map(() => undefined));
  }

  getPrecoDaVersao(projetoId: number, versaoId: number, comCalculo: boolean): Observable<PrecoDaVersaoDto | null> {
    // Sempre do servidor: o preço muda com a composição, os preços do projeto e o custo das peças.
    return this.apollo
      .query<GetPrecoDaVersaoResposta>({ query: GET_PRECO_DA_VERSAO, variables: { id: projetoId, versaoId, comCalculo }, fetchPolicy: 'network-only' })
      .pipe(
        dadosGql(),
        map((dados) => dados.projeto?.versoes[0]?.preco ?? null),
      );
  }

  putMetaisDaVersao(projetoId: number, versaoId: number, dto: PutMetaisDaVersaoDto): Observable<void> {
    return this.http.put(`projetos/${projetoId}/versoes/${versaoId}/metais`, dto).pipe(map(() => undefined));
  }

  putMaoDeObra(projetoId: number, versaoId: number, dto: PutMaoDeObraDto): Observable<void> {
    return this.http.put(`projetos/${projetoId}/versoes/${versaoId}/mao-de-obra`, { ...dto, tipo: TIPO_MAO_DE_OBRA_REST[dto.tipo] }).pipe(map(() => undefined));
  }

  putServicos(projetoId: number, versaoId: number, dto: PutServicosDto): Observable<void> {
    return this.http.put(`projetos/${projetoId}/versoes/${versaoId}/servicos`, dto).pipe(map(() => undefined));
  }

  putMarkupDaVersao(projetoId: number, versaoId: number, dto: PutMarkupDaVersaoDto): Observable<void> {
    return this.http.put(`projetos/${projetoId}/versoes/${versaoId}/markup`, dto).pipe(map(() => undefined));
  }

  /** Vale em qualquer status da versão (a publicada é a que vai para a bancada). */
  putPesoReal(projetoId: number, versaoId: number, ligaId: number, dto: PutPesoRealDto): Observable<void> {
    return this.http.put(`projetos/${projetoId}/versoes/${versaoId}/metais/${ligaId}/peso-real`, dto).pipe(map(() => undefined));
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

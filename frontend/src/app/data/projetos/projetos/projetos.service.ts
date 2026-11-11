import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { ConsultaPaginadaDto, PaginaDto } from '@data/common/graphql/pagina.dto';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { lerErroBlob } from '@shared/operators/ler-erro-blob.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { ArquivoFinalDaPecaDto, PecaDoProjetoDto, PesoDaPecaDto, PostPecaDoProjetoDto, PutPecaDoProjetoDto, PutValorDaPecaDto } from './dto/peca-do-projeto.dto';
import type { PrecoDaVersaoDto, PutMaoDeObraDto, PutMarkupDaVersaoDto, PutMetaisDaVersaoDto, PutPesoRealDto, PutServicosDto, TipoMaoDeObra } from './dto/preco-da-versao.dto';
import type { PostItemDto, PostProjetoDto, PostVersaoDto, PutItemDto, PutPrecosDoProjetoDto } from './dto/projeto-comandos.dto';
import type { PrecosDoProjetoDto, ProjetoDto, ProjetoResumoDto } from './dto/projeto.dto';

const GET_PROJETOS = gql`
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
      pecas {
        ...CamposDaPecaDoProjeto
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
            ...CamposDaPecaDoProjeto
          }
        }
      }
    }
  }

  fragment CamposDaPecaDoProjeto on PecaDoProjeto {
    id
    nome
    tipo
    material
    teor
    pesoGramas
    ativo
    status
  }
`;

/** Peças do projeto com a descrição e o vínculo; o `valor` só é pedido com `comValor` (para quem tem `precificacao.ler`). */
export const GET_PECAS_DO_PROJETO = gql`
  query GetPecasDoProjeto($id: Long!, $comValor: Boolean!) {
    projeto(id: $id) {
      id
      pecas {
        id
        nome
        tipo
        ligaId
        material
        teor
        pesoGramas
        descricao
        ativo
        status
        pecaId
        valor @include(if: $comValor)
      }
    }
  }
`;

/**
 * O peso de uma peça e de onde vem. Separada das outras leituras: com modelo 3D, o servidor mede o arquivo (a medição
 * fica guardada pelo arquivo), então só a peça pedida.
 */
export const GET_PESO_DA_PECA = gql`
  query GetPesoDaPeca($id: Long!, $pecaId: Long!) {
    projeto(id: $id) {
      id
      pecas(id: $pecaId) {
        id
        peso {
          gramas
          origem
          pesoDigitado
        }
      }
    }
  }
`;

/** O arquivo final de cada ferramenta da peça (o desenho que vale para o peso, a montagem e a publicação). */
const GET_ARQUIVOS_FINAIS_DA_PECA = gql`
  query GetArquivosFinaisDaPeca($id: Long!, $pecaId: Long!) {
    projeto(id: $id) {
      id
      pecas(id: $pecaId) {
        id
        arquivosFinais {
          ferramenta
          arquivoId
          nome
          criadoEm
        }
      }
    }
  }
`;

/** O peso que vale para cada peça da composição da versão (medido no 3D ou digitado), para o peso total. */
export const GET_PESOS_DA_VERSAO = gql`
  query GetPesosDaVersao($id: Long!, $versaoId: Long!) {
    projeto(id: $id) {
      id
      versoes(id: $versaoId) {
        id
        itens {
          id
          peca {
            id
            peso {
              gramas
              origem
            }
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
              pecaDoProjetoId
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
              pecaDoProjetoId
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

interface GetPecasDoProjetoResposta {
  readonly projeto: { readonly id: number; readonly pecas: readonly PecaDoProjetoDto[] } | null;
}

interface GetPesoDaPecaResposta {
  readonly projeto: { readonly pecas: readonly { readonly id: number; readonly peso: PesoDaPecaDto }[] } | null;
}

interface GetArquivosFinaisDaPecaResposta {
  readonly projeto: { readonly pecas: readonly { readonly id: number; readonly arquivosFinais: readonly ArquivoFinalDaPecaDto[] }[] } | null;
}

interface GetPesosDaVersaoResposta {
  readonly projeto: {
    readonly versoes: readonly { readonly itens: readonly { readonly peca: { readonly id: number; readonly peso: Pick<PesoDaPecaDto, 'gramas' | 'origem'> } }[] }[];
  } | null;
}

interface GetPrecosDoProjetoResposta {
  readonly projeto: { readonly id: number; readonly precos: PrecosDoProjetoDto } | null;
}

/**
 * Projetos, peças do projeto, versões e composição. Leitura pelo GraphQL; cada alteração é um command REST
 * (rotas aninhadas: /projetos/{id}/pecas/{pecaId} e /projetos/{id}/versoes/{versaoId}/itens/{itemId}).
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

  /** Nulo quando o projeto não existe. */
  getPecasDoProjeto(id: number, comValor: boolean): Observable<readonly PecaDoProjetoDto[] | null> {
    // Sempre do servidor: a tela cria, altera e publica peças e relê a lista.
    return this.apollo
      .query<GetPecasDoProjetoResposta>({ query: GET_PECAS_DO_PROJETO, variables: { id, comValor }, fetchPolicy: 'network-only' })
      .pipe(
        dadosGql(),
        map((dados) => dados.projeto?.pecas ?? null),
      );
  }

  /** Nulo quando a peça não é do projeto. Sempre do servidor: o modelo 3D da peça muda pelas ferramentas. */
  getPesoDaPeca(projetoId: number, pecaId: number): Observable<PesoDaPecaDto | null> {
    return this.apollo.query<GetPesoDaPecaResposta>({ query: GET_PESO_DA_PECA, variables: { id: projetoId, pecaId }, fetchPolicy: 'network-only' }).pipe(
      dadosGql(),
      map((dados) => dados.projeto?.pecas[0]?.peso ?? null),
    );
  }

  /** O arquivo final de cada ferramenta da peça; vazia sem final (ou se a peça não é do projeto). Sempre do servidor. */
  getArquivosFinaisDaPeca(projetoId: number, pecaId: number): Observable<readonly ArquivoFinalDaPecaDto[]> {
    return this.apollo
      .query<GetArquivosFinaisDaPecaResposta>({ query: GET_ARQUIVOS_FINAIS_DA_PECA, variables: { id: projetoId, pecaId }, fetchPolicy: 'network-only' })
      .pipe(
        dadosGql(),
        map((dados) => dados.projeto?.pecas[0]?.arquivosFinais ?? []),
      );
  }

  /** Gramas que valem para cada peça da composição (pelo Id da peça do projeto); sem peso, fica de fora. */
  getPesosDaVersao(projetoId: number, versaoId: number): Observable<ReadonlyMap<number, number>> {
    return this.apollo.query<GetPesosDaVersaoResposta>({ query: GET_PESOS_DA_VERSAO, variables: { id: projetoId, versaoId }, fetchPolicy: 'network-only' }).pipe(
      dadosGql(),
      map(
        (dados) =>
          new Map(
            (dados.projeto?.versoes[0]?.itens ?? [])
              .filter((i) => i.peca.peso.gramas !== null)
              .map((i) => [i.peca.id, i.peca.peso.gramas as number] as const),
          ),
      ),
    );
  }

  /** Nasce em rascunho, sem valor. */
  postPecaDoProjeto(projetoId: number, dto: PostPecaDoProjetoDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>(`projetos/${projetoId}/pecas`, dto);
  }

  /** Cópia publicada e vinculada à peça da biblioteca, sem valor. */
  postImportarPeca(projetoId: number, pecaId: number): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>(`projetos/${projetoId}/pecas/importar`, { pecaId });
  }

  /** Numa peça publicada, mudar algum dado faz dela outra peça (volta a rascunho). */
  putPecaDoProjeto(projetoId: number, pecaId: number, dto: PutPecaDoProjetoDto): Observable<void> {
    return this.http.put(`projetos/${projetoId}/pecas/${pecaId}`, dto).pipe(map(() => undefined));
  }

  /** Exige `precificacao.editar`. */
  putValorDaPeca(projetoId: number, pecaId: number, dto: PutValorDaPecaDto): Observable<void> {
    return this.http.put(`projetos/${projetoId}/pecas/${pecaId}/valor`, dto).pipe(map(() => undefined));
  }

  /**
   * Ficha técnica da peça em PDF, gerada na hora pelo servidor (nada é guardado). O valor no projeto só vem para quem
   * tem `precificacao.ler`. Em erro, o ProblemDetails chega como Blob; `lerErroBlob` converte.
   */
  getFichaTecnicaDaPeca(projetoId: number, pecaId: number): Observable<Blob> {
    return this.http.get(`projetos/${projetoId}/pecas/${pecaId}/documentos/ficha-tecnica.pdf`, { responseType: 'blob' }).pipe(lerErroBlob());
  }

  /** Coloca a peça em rascunho na biblioteca; devolve o Id da peça nova da biblioteca. */
  postPublicarPeca(projetoId: number, pecaId: number): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>(`projetos/${projetoId}/pecas/${pecaId}/publicar`, {});
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
    // Sempre do servidor: o preço muda com a composição, os preços do projeto e o valor das peças.
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

  putNome(projetoId: number, nome: string): Observable<void> {
    return this.http.put(`projetos/${projetoId}/nome`, { nome }).pipe(map(() => undefined));
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

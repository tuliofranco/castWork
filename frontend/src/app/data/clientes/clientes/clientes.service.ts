import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { ConsultaPaginadaDto, PaginaDto } from '@data/common/graphql/pagina.dto';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { ClienteOpcaoDto, ClienteResumoDto } from './dto/cliente-resumo.dto';
import type { PostClienteDto } from './dto/post-cliente.dto';

const GET_CLIENTES = gql`
  query GetClientes($termo: String, $primeiros: Int, $apos: String) {
    clientes(termo: $termo, first: $primeiros, after: $apos) {
      totalCount
      pageInfo {
        hasNextPage
        endCursor
      }
      nodes {
        id
        nome
        documento
        email
        telefone
        ativo
        projetos {
          id
        }
      }
    }
  }
`;

export const GET_CLIENTES_OPCOES = gql`
  query GetClientesOpcoes($termo: String) {
    clientes(termo: $termo, first: 50, where: { ativo: { eq: true } }) {
      nodes {
        id
        nome
      }
    }
  }
`;

interface GetClientesResposta {
  readonly clientes: PaginaDto<ClienteResumoDto>;
}

interface GetClientesOpcoesResposta {
  readonly clientes: { readonly nodes: readonly ClienteOpcaoDto[] };
}

@Injectable({ providedIn: 'root' })
export class ClientesService {
  private readonly apollo = inject(Apollo);
  private readonly http = inject(HttpClient);

  getClientes(consulta: ConsultaPaginadaDto): Observable<PaginaDto<ClienteResumoDto>> {
    return this.apollo
      .query<GetClientesResposta>({ query: GET_CLIENTES, variables: { ...consulta } })
      .pipe(
        dadosGql(),
        map((dados) => dados.clientes),
      );
  }

  /** Até 50 clientes ativos por nome, para o seletor do formulário de peça. */
  getClientesOpcoes(termo?: string): Observable<readonly ClienteOpcaoDto[]> {
    return this.apollo
      .query<GetClientesOpcoesResposta>({ query: GET_CLIENTES_OPCOES, variables: { termo: termo ?? null } })
      .pipe(
        dadosGql(),
        map((dados) => dados.clientes.nodes),
      );
  }

  postCliente(dto: PostClienteDto): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>('clientes', dto);
  }
}

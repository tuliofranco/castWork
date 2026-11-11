import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { dadosGql } from '@shared/operators/dados-gql.operator';
import { Apollo, gql } from 'apollo-angular';
import { type Observable, map } from 'rxjs';

import type { ConfiguracaoAtelieDto, PutAtelieDto } from './dto/atelie.dto';

const GET_CONFIGURACAO_ATELIE = gql`
  query GetConfiguracaoAtelie {
    configuracaoAtelie {
      nome
      documento
      email
      telefone
      endereco {
        cep
        logradouro
        numero
        complemento
        bairro
        cidade
        uf
      }
      assinatura
      corMarca
      instagram
      site
      avisoBriefing
      possuiLogo
    }
  }
`;

interface GetConfiguracaoAtelieResposta {
  readonly configuracaoAtelie: ConfiguracaoAtelieDto;
}

/**
 * Dados e marca do ateliê: leitura pelo GraphQL; escrita e logo (binário) por REST.
 */
@Injectable({ providedIn: 'root' })
export class AtelieService {
  private readonly http = inject(HttpClient);
  private readonly apollo = inject(Apollo);

  getConfiguracao(): Observable<ConfiguracaoAtelieDto> {
    // Sempre do servidor: a tela salva e relê, e o cache do Apollo mostraria o valor antigo.
    return this.apollo
      .query<GetConfiguracaoAtelieResposta>({ query: GET_CONFIGURACAO_ATELIE, fetchPolicy: 'network-only' })
      .pipe(
        dadosGql(),
        map((dados) => dados.configuracaoAtelie),
      );
  }

  putConfiguracao(dto: PutAtelieDto): Observable<void> {
    return this.http.put('atelie', dto).pipe(map(() => undefined));
  }

  getLogo(): Observable<Blob> {
    return this.http.get('atelie/logo', { responseType: 'blob' });
  }

  putLogo(arquivo: File): Observable<void> {
    const formulario = new FormData();
    formulario.append('arquivo', arquivo);
    return this.http.put('atelie/logo', formulario).pipe(map(() => undefined));
  }

  deleteLogo(): Observable<void> {
    return this.http.delete('atelie/logo').pipe(map(() => undefined));
  }
}

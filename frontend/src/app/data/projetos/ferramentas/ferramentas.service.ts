import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { type Observable, map } from 'rxjs';

import type { ConversaFerramentaDto, VersaoDoArquivoDto } from './dto/ferramenta.dto';

/** Arquivo de trabalho da ferramenta: conteúdo e a versão (Id) dele. */
export interface ArquivoFerramenta {
  readonly id: number;
  readonly conteudo: Blob;
}

/**
 * Ferramentas com IA de uma versão (ex.: `desenho-2d`): a conversa daquela ferramenta e o arquivo de trabalho.
 */
@Injectable({ providedIn: 'root' })
export class FerramentasService {
  private readonly http = inject(HttpClient);

  getConversa(projetoId: number, versaoId: number, ferramenta: string): Observable<ConversaFerramentaDto> {
    return this.http.get<ConversaFerramentaDto>(`${this.base(projetoId, versaoId, ferramenta)}/conversa`);
  }

  /** `imagens` em base64, já reduzidas no tablet (ver `prepararImagem`). */
  postConversa(projetoId: number, versaoId: number, ferramenta: string, texto: string | null, imagens: readonly string[]): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, versaoId, ferramenta)}/conversa`, { texto, imagens });
  }

  deleteConversa(projetoId: number, versaoId: number, ferramenta: string): Observable<void> {
    return this.http.delete(`${this.base(projetoId, versaoId, ferramenta)}/conversa`).pipe(map(() => undefined));
  }

  postDesfazer(projetoId: number, versaoId: number, ferramenta: string, ordem: number): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, versaoId, ferramenta)}/conversa/mensagens/${ordem}/desfazer`, null);
  }

  /** Nulo quando a ferramenta ainda não tem arquivo (204). */
  getArquivo(projetoId: number, versaoId: number, ferramenta: string): Observable<ArquivoFerramenta | null> {
    return this.http
      .get(`${this.base(projetoId, versaoId, ferramenta)}/arquivo`, { observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : { id: Number(r.headers.get('X-Arquivo-Id')), conteudo: r.body })));
  }

  /** `conteudo` em base64. Devolve o Id da nova versão do arquivo. */
  putArquivo(projetoId: number, versaoId: number, ferramenta: string, conteudo: string): Observable<number> {
    return this.http.put<IdCriadoDto>(`${this.base(projetoId, versaoId, ferramenta)}/arquivo`, { conteudo }).pipe(map((r) => r.id));
  }

  /** Versões do arquivo, da atual para a mais antiga (sem o conteúdo). */
  getHistorico(projetoId: number, versaoId: number, ferramenta: string): Observable<readonly VersaoDoArquivoDto[]> {
    return this.http.get<readonly VersaoDoArquivoDto[]>(`${this.base(projetoId, versaoId, ferramenta)}/arquivo/historico`);
  }

  /** Nulo quando a versão é o desenho em branco (204). */
  getVersaoDoArquivo(projetoId: number, versaoId: number, ferramenta: string, arquivoId: number): Observable<Blob | null> {
    return this.http
      .get(`${this.base(projetoId, versaoId, ferramenta)}/arquivo/historico/${arquivoId}`, { observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : r.body)));
  }

  /** Uma cópia da versão vira a atual. Devolve o Id do arquivo atual (nulo se for o desenho em branco). */
  postRestaurar(projetoId: number, versaoId: number, ferramenta: string, arquivoId: number): Observable<number | null> {
    return this.http
      .post<{ arquivoId: number | null }>(`${this.base(projetoId, versaoId, ferramenta)}/arquivo/historico/${arquivoId}/restaurar`, null)
      .pipe(map((r) => r.arquivoId));
  }

  /** Caminho da API de uma imagem das conversas da versão (para `cw-imagem-protegida`). */
  caminhoDaImagem(projetoId: number, versaoId: number, chave: string): string {
    return `projetos/${projetoId}/versoes/${versaoId}/ferramentas/imagens/${chave}`;
  }

  private base(projetoId: number, versaoId: number, ferramenta: string): string {
    return `projetos/${projetoId}/versoes/${versaoId}/ferramentas/${ferramenta}`;
  }
}

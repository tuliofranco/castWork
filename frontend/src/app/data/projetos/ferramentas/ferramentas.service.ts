import { HttpClient, type HttpEvent, HttpEventType } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { semLoading } from '@shared/injection-tokens/http-context.tokens';
import { Observable, map } from 'rxjs';

import type { ChatDaConversaDto, ConversaFerramentaDto, EventoDoTurnoDto, ListaDeArquivosDto, VersaoDoArquivoDto } from './dto/ferramenta.dto';

/** Arquivo de trabalho da ferramenta: conteúdo, a versão (Id) dele e o nome dado pelo joalheiro. */
export interface ArquivoFerramenta {
  readonly id: number;
  readonly conteudo: Blob;
  readonly nome: string | null;
}

/**
 * Ferramentas com IA de uma versão (ex.: `desenho-2d`): a conversa daquela ferramenta e o arquivo de trabalho.
 */
@Injectable({ providedIn: 'root' })
export class FerramentasService {
  private readonly http = inject(HttpClient);

  /** Sem a barra de carregamento global: enquanto a IA trabalha, quem mostra o andamento é o chat. */
  getConversa(projetoId: number, versaoId: number | null, ferramenta: string): Observable<ConversaFerramentaDto> {
    return this.http.get<ConversaFerramentaDto>(`${this.base(projetoId, versaoId, ferramenta)}/conversa`, { context: semLoading() });
  }

  /**
   * `imagens` em base64, já reduzidas no tablet (ver `prepararImagem`). Pode levar minutos: o andamento vem
   * por `acompanhar`, não pela barra de carregamento global.
   */
  postConversa(projetoId: number, versaoId: number | null, ferramenta: string, texto: string | null, imagens: readonly string[]): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, versaoId, ferramenta)}/conversa`, { texto, imagens }, { context: semLoading() });
  }

  /**
   * O turno da IA ao vivo (Server-Sent Events): pensamento, passos e texto até o `fim`. Pelo HttpClient (e
   * não EventSource) para levar o token e a renovação dele; cada trecho recebido é lido do texto parcial.
   * `aguardar`: aberto junto com o envio da mensagem, espera o turno começar no servidor.
   */
  acompanhar(projetoId: number, versaoId: number | null, ferramenta: string, aguardar: boolean): Observable<EventoDoTurnoDto> {
    const url = `${this.base(projetoId, versaoId, ferramenta)}/conversa/eventos${aguardar ? '?aguardar=true' : ''}`;
    return new Observable<EventoDoTurnoDto>((assinante) => {
      let lido = 0;
      let resto = '';
      const entregar = (texto: string) => {
        resto += texto;
        const blocos = resto.split('\n\n');
        resto = blocos.pop() ?? '';
        for (const bloco of blocos) {
          const dados = bloco.split('\n').filter((l) => l.startsWith('data: ')).map((l) => l.slice(6)).join('\n');
          if (!dados) continue;
          const evento = JSON.parse(dados) as EventoDoTurnoDto;
          assinante.next(evento);
          if (evento.tipo === 'fim') assinante.complete();
        }
      };

      const assinatura = this.http
        .get(url, { observe: 'events', reportProgress: true, responseType: 'text', context: semLoading() })
        .subscribe({
          next: (evento: HttpEvent<string>) => {
            let parcial: string | null | undefined = null;
            if (evento.type === HttpEventType.DownloadProgress) parcial = evento.partialText;
            else if (evento.type === HttpEventType.Response) parcial = evento.body;
            if (parcial == null || parcial.length <= lido) return;
            entregar(parcial.slice(lido));
            lido = parcial.length;
          },
          error: (erro: unknown) => assinante.error(erro),
          complete: () => assinante.complete(),
        });
      return () => assinatura.unsubscribe();
    });
  }

  getChats(projetoId: number, versaoId: number | null, ferramenta: string): Observable<readonly ChatDaConversaDto[]> {
    return this.http.get<readonly ChatDaConversaDto[]>(`${this.base(projetoId, versaoId, ferramenta)}/conversa/chats`);
  }

  /** Novo chat: o atual vai para o histórico; o desenho continua o mesmo. */
  postNovoChat(projetoId: number, versaoId: number | null, ferramenta: string): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, versaoId, ferramenta)}/conversa/chats`, null);
  }

  postContinuarChat(projetoId: number, versaoId: number | null, ferramenta: string, numero: number): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, versaoId, ferramenta)}/conversa/chats/${numero}/continuar`, null);
  }

  deleteConversa(projetoId: number, versaoId: number | null, ferramenta: string): Observable<void> {
    return this.http.delete(`${this.base(projetoId, versaoId, ferramenta)}/conversa`).pipe(map(() => undefined));
  }

  postDesfazer(projetoId: number, versaoId: number | null, ferramenta: string, ordem: number): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, versaoId, ferramenta)}/conversa/mensagens/${ordem}/desfazer`, null);
  }

  /** Nulo quando a ferramenta ainda não tem arquivo (204). */
  getArquivo(projetoId: number, versaoId: number, ferramenta: string): Observable<ArquivoFerramenta | null> {
    return this.http
      .get(`${this.base(projetoId, versaoId, ferramenta)}/arquivo`, { observe: 'response', responseType: 'blob' })
      .pipe(
        map((r) => {
          if (r.status === 204 || !r.body) return null;
          const nome = r.headers.get('X-Arquivo-Nome');
          return { id: Number(r.headers.get('X-Arquivo-Id')), conteudo: r.body, nome: nome ? decodeURIComponent(nome) : null };
        }),
      );
  }

  /** `conteudo` em base64. Devolve o Id da nova versão do arquivo. */
  putArquivo(projetoId: number, versaoId: number, ferramenta: string, conteudo: string): Observable<number> {
    return this.http.put<IdCriadoDto>(`${this.base(projetoId, versaoId, ferramenta)}/arquivo`, { conteudo }).pipe(map((r) => r.id));
  }

  /** Prévia (SVG) de um arquivo do histórico; nula se não há prévia (204). */
  getPrevia(projetoId: number, versaoId: number, ferramenta: string, arquivoId: number): Observable<Blob | null> {
    return this.http
      .get(`${this.base(projetoId, versaoId, ferramenta)}/arquivo/historico/${arquivoId}/previa`, { observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : r.body)));
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

  /** Os arquivos da versão, de todas as ferramentas, do mais recente para o mais antigo. */
  getArquivosDaVersao(projetoId: number, versaoId: number, arquivados: boolean, pagina: number, tamanhoPagina: number): Observable<ListaDeArquivosDto> {
    return this.http.get<ListaDeArquivosDto>(`projetos/${projetoId}/versoes/${versaoId}/ferramentas/arquivos`, { params: { arquivados, pagina, tamanhoPagina } });
  }

  /** Nome vazio volta ao padrão da ferramenta. */
  putNomeDoArquivo(projetoId: number, versaoId: number, ferramenta: string, arquivoId: number, nome: string | null): Observable<void> {
    return this.http.put(`${this.base(projetoId, versaoId, ferramenta)}/arquivo/historico/${arquivoId}/nome`, { nome }).pipe(map(() => undefined));
  }

  postArquivar(projetoId: number, versaoId: number, ferramenta: string, arquivoId: number): Observable<void> {
    return this.http.post(`${this.base(projetoId, versaoId, ferramenta)}/arquivo/historico/${arquivoId}/arquivar`, null).pipe(map(() => undefined));
  }

  postDesarquivar(projetoId: number, versaoId: number, ferramenta: string, arquivoId: number): Observable<void> {
    return this.http.post(`${this.base(projetoId, versaoId, ferramenta)}/arquivo/historico/${arquivoId}/desarquivar`, null).pipe(map(() => undefined));
  }

  /** Definitivo; só para arquivado. */
  deleteArquivo(projetoId: number, versaoId: number, ferramenta: string, arquivoId: number): Observable<void> {
    return this.http.delete(`${this.base(projetoId, versaoId, ferramenta)}/arquivo/historico/${arquivoId}`).pipe(map(() => undefined));
  }

  /** Caminho da API de uma imagem das conversas (para `cw-imagem-protegida`); sem versão, as do projeto. */
  caminhoDaImagem(projetoId: number, versaoId: number | null, chave: string): string {
    return versaoId === null ? `projetos/${projetoId}/ferramentas/imagens/${chave}` : `projetos/${projetoId}/versoes/${versaoId}/ferramentas/imagens/${chave}`;
  }

  /** Sem versão: as ferramentas do projeto (o briefing vale para todas as versões). */
  private base(projetoId: number, versaoId: number | null, ferramenta: string): string {
    return versaoId === null ? `projetos/${projetoId}/ferramentas/${ferramenta}` : `projetos/${projetoId}/versoes/${versaoId}/ferramentas/${ferramenta}`;
  }
}

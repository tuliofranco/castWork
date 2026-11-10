import { HttpClient, type HttpEvent, HttpEventType } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { semLoading } from '@shared/injection-tokens/http-context.tokens';
import { Observable, map } from 'rxjs';

import type {
  AnexoDoProjetoDto,
  ArquivoFinalDto,
  ChatDaConversaDto,
  ConversaFerramentaDto,
  EventoDoTurnoDto,
  ImagensDoArquivoDto,
  ListaDeArquivosDto,
  VersaoDoArquivoDto,
  VistaDaImagem,
} from './dto/ferramenta.dto';

/**
 * De quem é a conversa da ferramenta: uma versão (o Id dela), uma peça do projeto (`{ pecaId }`: o desenho 2D e o modelo
 * 3D de cada peça) ou o próprio projeto (`null`: o briefing, que vale para todas as versões).
 */
export type EscopoDaFerramenta = number | { readonly pecaId: number } | null;

/** Arquivo de trabalho da ferramenta: conteúdo, a versão (Id) dele e o nome dado pelo joalheiro. */
export interface ArquivoFerramenta {
  readonly id: number;
  readonly conteudo: Blob;
  readonly nome: string | null;
}

/**
 * Ferramentas com IA de uma versão, de uma peça do projeto ou do projeto (ex.: `desenho-2d`): a conversa daquela
 * ferramenta e o arquivo de trabalho.
 */
@Injectable({ providedIn: 'root' })
export class FerramentasService {
  private readonly http = inject(HttpClient);

  /** Sem a barra de carregamento global: enquanto a IA trabalha, quem mostra o andamento é o chat. */
  getConversa(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string): Observable<ConversaFerramentaDto> {
    return this.http.get<ConversaFerramentaDto>(`${this.base(projetoId, escopo, ferramenta)}/conversa`, { context: semLoading() });
  }

  /**
   * `imagens` em base64, já reduzidas no tablet (ver `prepararImagem`). Pode levar minutos: o andamento vem
   * por `acompanhar`, não pela barra de carregamento global.
   */
  postConversa(
    projetoId: number,
    escopo: EscopoDaFerramenta,
    ferramenta: string,
    texto: string | null,
    imagens: readonly string[],
    anexos: readonly { readonly origem: string; readonly id: number }[] = [],
    ia: { readonly modelo?: string; readonly esforco?: string } = {},
  ): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, escopo, ferramenta)}/conversa`, { texto, imagens, anexos, ...ia }, { context: semLoading() });
  }

  /**
   * O turno da IA ao vivo (Server-Sent Events): pensamento, passos e texto até o `fim`. Pelo HttpClient (e
   * não EventSource) para levar o token e a renovação dele; cada trecho recebido é lido do texto parcial.
   * `aguardar`: aberto junto com o envio da mensagem, espera o turno começar no servidor.
   */
  acompanhar(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, aguardar: boolean): Observable<EventoDoTurnoDto> {
    const url = `${this.base(projetoId, escopo, ferramenta)}/conversa/eventos${aguardar ? '?aguardar=true' : ''}`;
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

  getChats(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string): Observable<readonly ChatDaConversaDto[]> {
    return this.http.get<readonly ChatDaConversaDto[]>(`${this.base(projetoId, escopo, ferramenta)}/conversa/chats`);
  }

  /** Novo chat: o atual vai para o histórico; o desenho continua o mesmo. */
  postNovoChat(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, escopo, ferramenta)}/conversa/chats`, null);
  }

  postContinuarChat(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, numero: number): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, escopo, ferramenta)}/conversa/chats/${numero}/continuar`, null);
  }

  deleteConversa(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string): Observable<void> {
    return this.http.delete(`${this.base(projetoId, escopo, ferramenta)}/conversa`).pipe(map(() => undefined));
  }

  postDesfazer(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, ordem: number): Observable<ConversaFerramentaDto> {
    return this.http.post<ConversaFerramentaDto>(`${this.base(projetoId, escopo, ferramenta)}/conversa/mensagens/${ordem}/desfazer`, null);
  }

  /** Nulo quando a ferramenta ainda não tem arquivo (204). `final`: o arquivo final da peça no lugar do atual. */
  getArquivo(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, final = false): Observable<ArquivoFerramenta | null> {
    return this.http
      .get(`${this.base(projetoId, escopo, ferramenta)}/arquivo`, { params: paramsDoFinal(final), observe: 'response', responseType: 'blob' })
      .pipe(
        map((r) => {
          if (r.status === 204 || !r.body) return null;
          const nome = r.headers.get('X-Arquivo-Nome');
          return { id: Number(r.headers.get('X-Arquivo-Id')), conteudo: r.body, nome: nome ? decodeURIComponent(nome) : null };
        }),
      );
  }

  /** `conteudo` em base64. Devolve o Id da nova versão do arquivo. */
  putArquivo(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, conteudo: string): Observable<number> {
    return this.http.put<IdCriadoDto>(`${this.base(projetoId, escopo, ferramenta)}/arquivo`, { conteudo }).pipe(map((r) => r.id));
  }

  /** Prévia (SVG) do arquivo atual da ferramenta (ou do final da peça, com `final`); nulo se não há arquivo ou prévia. */
  getPreviaAtual(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, final = false): Observable<Blob | null> {
    return this.http
      .get(`${this.base(projetoId, escopo, ferramenta)}/arquivo/previa`, { params: paramsDoFinal(final), observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : r.body)));
  }

  /** Prévia (SVG) de um arquivo do histórico; nula se não há prévia (204). */
  getPrevia(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, arquivoId: number): Observable<Blob | null> {
    return this.http
      .get(`${this.base(projetoId, escopo, ferramenta)}/arquivo/historico/${arquivoId}/previa`, { observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : r.body)));
  }

  /** As imagens guardadas do arquivo atual (ou do final da peça, com `final`); vazia enquanto não foram geradas. */
  getImagensDoArquivo(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, final = false): Observable<ImagensDoArquivoDto> {
    return this.http.get<ImagensDoArquivoDto>(`${this.base(projetoId, escopo, ferramenta)}/arquivo/imagens`, { params: paramsDoFinal(final) });
  }

  /**
   * Uma imagem guardada do arquivo atual (ou do final da peça, com `final`); sem `formato`, a de melhor qualidade (o SVG do
   * 2D). Nula se não há (204).
   */
  getImagemDoArquivo(
    projetoId: number,
    escopo: EscopoDaFerramenta,
    ferramenta: string,
    vista: VistaDaImagem,
    formato?: 'png' | 'svg',
    final = false,
  ): Observable<Blob | null> {
    const params: Record<string, string> = { ...(formato ? { formato } : {}), ...paramsDoFinal(final) };
    return this.http
      .get(`${this.base(projetoId, escopo, ferramenta)}/arquivo/imagens/${vista}`, { params, observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : r.body)));
  }

  /** Versões do arquivo, da atual para a mais antiga (sem o conteúdo). */
  getHistorico(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string): Observable<readonly VersaoDoArquivoDto[]> {
    return this.http.get<readonly VersaoDoArquivoDto[]>(`${this.base(projetoId, escopo, ferramenta)}/arquivo/historico`);
  }

  /** Nulo quando a versão é o desenho em branco (204). */
  getVersaoDoArquivo(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, arquivoId: number): Observable<Blob | null> {
    return this.http
      .get(`${this.base(projetoId, escopo, ferramenta)}/arquivo/historico/${arquivoId}`, { observe: 'response', responseType: 'blob' })
      .pipe(map((r) => (r.status === 204 || !r.body ? null : r.body)));
  }

  /** Uma cópia da versão vira a atual. Devolve o Id do arquivo atual (nulo se for o desenho em branco). */
  postRestaurar(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, arquivoId: number): Observable<number | null> {
    return this.http
      .post<{ arquivoId: number | null }>(`${this.base(projetoId, escopo, ferramenta)}/arquivo/historico/${arquivoId}/restaurar`, null)
      .pipe(map((r) => r.arquivoId));
  }

  /** Os arquivos da versão, de todas as ferramentas, do mais recente para o mais antigo. */
  /** O que pode ser anexado "Do projeto" numa conversa (arquivos das ferramentas e o PDF do briefing). */
  getAnexosDoProjeto(projetoId: number): Observable<readonly AnexoDoProjetoDto[]> {
    return this.http.get<readonly AnexoDoProjetoDto[]>(`projetos/${projetoId}/anexos`);
  }

  getArquivosDaVersao(projetoId: number, versaoId: number, arquivados: boolean, pagina: number, tamanhoPagina: number): Observable<ListaDeArquivosDto> {
    return this.http.get<ListaDeArquivosDto>(`projetos/${projetoId}/versoes/${versaoId}/ferramentas/arquivos`, { params: { arquivados, pagina, tamanhoPagina } });
  }

  /**
   * Marca o arquivo como o final da peça (o desenho dela naquela ferramenta). No 3D, os corpos recebem o nome da peça numa
   * nova versão do arquivo (`ajustado`). Numa peça publicada, trocar o final faz dela outra peça.
   */
  postMarcarFinal(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, arquivoId: number): Observable<ArquivoFinalDto> {
    return this.http.post<ArquivoFinalDto>(`${this.base(projetoId, escopo, ferramenta)}/arquivo/historico/${arquivoId}/final`, null);
  }

  /** Nome vazio volta ao padrão da ferramenta. */
  putNomeDoArquivo(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, arquivoId: number, nome: string | null): Observable<void> {
    return this.http.put(`${this.base(projetoId, escopo, ferramenta)}/arquivo/historico/${arquivoId}/nome`, { nome }).pipe(map(() => undefined));
  }

  postArquivar(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, arquivoId: number): Observable<void> {
    return this.http.post(`${this.base(projetoId, escopo, ferramenta)}/arquivo/historico/${arquivoId}/arquivar`, null).pipe(map(() => undefined));
  }

  postDesarquivar(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, arquivoId: number): Observable<void> {
    return this.http.post(`${this.base(projetoId, escopo, ferramenta)}/arquivo/historico/${arquivoId}/desarquivar`, null).pipe(map(() => undefined));
  }

  /** Definitivo; só para arquivado. */
  deleteArquivo(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string, arquivoId: number): Observable<void> {
    return this.http.delete(`${this.base(projetoId, escopo, ferramenta)}/arquivo/historico/${arquivoId}`).pipe(map(() => undefined));
  }

  /** Caminho da API de uma imagem das conversas (para `cw-imagem-protegida`); sem versão nem peça, as do projeto. */
  caminhoDaImagem(projetoId: number, escopo: EscopoDaFerramenta, chave: string): string {
    return `${this.raiz(projetoId, escopo)}/ferramentas/imagens/${chave}`;
  }

  /** Sem versão nem peça: as ferramentas do projeto (o briefing vale para todas as versões). */
  private base(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string): string {
    return `${this.raiz(projetoId, escopo)}/ferramentas/${ferramenta}`;
  }

  private raiz(projetoId: number, escopo: EscopoDaFerramenta): string {
    if (escopo === null) return `projetos/${projetoId}`;
    return typeof escopo === 'number' ? `projetos/${projetoId}/versoes/${escopo}` : `projetos/${projetoId}/pecas/${escopo.pecaId}`;
  }
}

/** `?final=true` só quando pedido: sem ele, a API devolve o arquivo atual. */
function paramsDoFinal(final: boolean): Record<string, string> {
  return final ? { final: 'true' } : {};
}

import type { PapelMensagem } from '@data/ferramentas/briefing/dto/briefing.dto';

export interface AcaoDaIaDto {
  readonly descricao: string;
  readonly desfeita: boolean;
}

export interface MensagemFerramentaDto {
  readonly ordem: number;
  readonly papel: PapelMensagem;
  readonly texto: string | null;
  /** Chaves das imagens (anexadas pelo joalheiro ou o render que a IA olhou). */
  readonly imagens: readonly string[];
  readonly acao: AcaoDaIaDto | null;
  readonly criadaEm: string;
  /** Resposta final da IA: resumo do que ela pensou no turno. */
  readonly pensamento?: string | null;
  /** Resposta final da IA: o que ela fez, em passos legíveis. */
  readonly passos?: readonly string[] | null;
  /** Resposta final da IA: tokens que o turno consumiu. */
  readonly tokens?: number | null;
  /** Arquivos do projeto anexados à fala. */
  readonly anexos?: readonly AnexoDaMensagemDto[] | null;
}

/** Um arquivo do projeto anexado à fala: a origem (`arquivo` de ferramenta ou `briefing`), o id e o nome. */
export interface AnexoDaMensagemDto {
  readonly origem: string;
  readonly id: number;
  readonly nome: string;
}

/**
 * Um arquivo que pode ser anexado "Do projeto": o atual de cada ferramenta (de todas as versões) e o último
 * PDF do briefing. `versao` é o número da versão; nulo no que é do projeto.
 */
export interface AnexoDoProjetoDto {
  readonly origem: string;
  readonly id: number;
  readonly nome: string;
  readonly ferramenta: string;
  readonly versao: number | null;
  readonly criadoEm: string;
  readonly tamanho: number;
}

/**
 * Um acontecimento do turno da IA, ao vivo. `pensamento` e `texto` são trechos que se somam; `passo` começa
 * (andamento) e termina (feito/erro) uma ferramenta, às vezes com o render (`imagemPng`, base64); `fim` encerra.
 */
export interface EventoDoTurnoDto {
  readonly tipo: 'pensamento' | 'texto' | 'passo' | 'fim';
  readonly texto?: string | null;
  readonly passo?: number | null;
  readonly estado?: 'andamento' | 'feito' | 'erro' | null;
  readonly imagemPng?: string | null;
}

export interface ConversaFerramentaDto {
  readonly mensagens: readonly MensagemFerramentaDto[];
  /** Versão atual do arquivo; muda quando a IA desenha, quando se desfaz ou quando se salva. */
  readonly arquivoId: number | null;
  /** A IA ainda trabalha na última fala (o joalheiro saiu e voltou no meio do turno). */
  readonly processando: boolean;
  /** Número do chat mostrado. */
  readonly chat: number;
  /** Quantos outros chats estão no histórico. */
  readonly chatsAnteriores: number;
}

/** Um chat da conversa, para a lista de conversas anteriores (título: começo da primeira fala). */
export interface ChatDaConversaDto {
  readonly numero: number;
  readonly iniciadoEm: string;
  readonly ultimaMensagemEm: string;
  readonly titulo: string | null;
  readonly mensagens: number;
  readonly atual: boolean;
}

/** Quem gravou a versão do arquivo. */
export type OrigemArquivo = 'Ia' | 'Joalheiro' | 'Desfazer' | 'Restauracao' | 'Conferencia' | 'Biblioteca' | 'Montagem' | 'NomesDaPeca';

/** Vista de uma imagem guardada: as quatro do modelo 3D e a do desenho 2D. */
export type VistaDaImagem = 'iso' | 'frente' | 'topo' | 'lado' | '2d';

/** Uma imagem guardada do arquivo (gerada em alta qualidade depois de salvar): vista e tipo (`image/png` ou `image/svg+xml`). */
export interface ImagemGuardadaDto {
  readonly vista: VistaDaImagem;
  readonly tipo: string;
}

/** As imagens guardadas do arquivo atual, na ordem de mostrar; vazia enquanto não foram geradas. */
export interface ImagensDoArquivoDto {
  readonly arquivoId: number | null;
  readonly imagens: readonly ImagemGuardadaDto[];
}

/** Uma versão do histórico do arquivo; `tamanho` 0 é o desenho em branco. */
export interface VersaoDoArquivoDto {
  readonly id: number;
  readonly origem: OrigemArquivo;
  readonly criadoEm: string;
  readonly tamanho: number;
  readonly nome: string | null;
}

/** Um arquivo da pasta da versão (de qualquer ferramenta). */
export interface ArquivoDaVersaoDto {
  readonly id: number;
  /** Código da ferramenta (`desenho-2d`). */
  readonly ferramenta: string;
  readonly origem: OrigemArquivo;
  readonly criadoEm: string;
  readonly tamanho: number;
  /** Nome dado pelo joalheiro; nulo usa o padrão da ferramenta. */
  readonly nome: string | null;
  readonly arquivadoEm: string | null;
}

export interface ListaDeArquivosDto {
  readonly lista: readonly ArquivoDaVersaoDto[];
  readonly pagina: number;
  readonly tamanhoPagina: number;
  readonly totalRegistros: number;
  readonly totalPaginas: number;
}

/** O arquivo que ficou como final da peça (o ajustado, quando `ajustado`: a tela recarrega) e se a peça publicada virou outra. */
export interface ArquivoFinalDto {
  readonly arquivoId: number;
  readonly ajustado: boolean;
  readonly virouOutraPeca: boolean;
}

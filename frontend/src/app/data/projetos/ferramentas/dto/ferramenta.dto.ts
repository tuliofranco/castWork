import type { PapelMensagem } from '@data/projetos/briefing/dto/briefing.dto';

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
}

export interface ConversaFerramentaDto {
  readonly mensagens: readonly MensagemFerramentaDto[];
  /** Versão atual do arquivo; muda quando a IA desenha, quando se desfaz ou quando se salva. */
  readonly arquivoId: number | null;
}

/** Quem gravou a versão do arquivo. */
export type OrigemArquivo = 'Ia' | 'Joalheiro' | 'Desfazer' | 'Restauracao';

/** Uma versão do histórico do arquivo; `tamanho` 0 é o desenho em branco. */
export interface VersaoDoArquivoDto {
  readonly id: number;
  readonly origem: OrigemArquivo;
  readonly criadoEm: string;
  readonly tamanho: number;
}

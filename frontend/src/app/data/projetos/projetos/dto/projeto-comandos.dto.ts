import type { Metal, PutParametrosDePrecoDto } from '@data/precificacao/precos/dto/precos.dto';

export interface PostProjetoDto {
  readonly nome: string;
  readonly descricao?: string | null;
  readonly clienteId?: number | null;
}

export interface PostVersaoDto {
  readonly nome: string;
  readonly observacoes?: string | null;
}

export interface PostItemDto {
  /** Peça ativa deste projeto. */
  readonly pecaDoProjetoId: number;
  readonly quantidade: number;
  readonly observacao?: string | null;
}

export interface PutItemDto {
  readonly quantidade: number;
  readonly observacao?: string | null;
}

/** Substitui cotações (metal ausente fica sem cotação) e parâmetros só neste projeto. */
export interface PutPrecosDoProjetoDto extends PutParametrosDePrecoDto {
  readonly cotacoes: readonly { readonly metal: Metal; readonly valor: number }[];
}

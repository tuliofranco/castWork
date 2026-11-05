import type { PecaDto } from '@data/pecas/pecas/dto/peca.dto';

export type StatusVersao = 'EM_DESENVOLVIMENTO' | 'PUBLICADA';

/** Linha da listagem de projetos. */
export interface ProjetoResumoDto {
  readonly id: number;
  readonly nome: string;
  readonly cliente: { readonly id: number; readonly nome: string } | null;
  /** Da mais recente para a mais antiga. */
  readonly versoes: readonly { readonly id: number; readonly numero: number; readonly status: StatusVersao }[];
}

export interface ItemComposicaoDto {
  readonly id: number;
  readonly quantidade: number;
  readonly observacao: string | null;
  readonly peca: Pick<PecaDto, 'id' | 'nome' | 'tipo' | 'material' | 'teor' | 'pesoGramas'>;
}

export interface VersaoProjetoDto {
  readonly id: number;
  readonly numero: number;
  readonly nome: string;
  readonly observacoes: string | null;
  readonly status: StatusVersao;
  readonly publicadaEm: string | null;
  readonly itens: readonly ItemComposicaoDto[];
}

export interface ProjetoDto {
  readonly id: number;
  readonly nome: string;
  readonly descricao: string | null;
  readonly cliente: { readonly id: number; readonly nome: string } | null;
  readonly versoes: readonly VersaoProjetoDto[];
}

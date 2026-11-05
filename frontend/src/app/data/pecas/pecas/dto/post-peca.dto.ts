import type { TipoPeca } from '@shared/pecas/tipo-peca';

export interface PostPecaDto {
  readonly nome: string;
  readonly tipo: TipoPeca;
  readonly material?: string | null;
  /** "18k" ou "750". */
  readonly teor?: string | null;
  readonly pesoGramas?: number | null;
  readonly descricao?: string | null;
}

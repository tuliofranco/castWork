import type { TipoPeca } from '@shared/pecas/tipo-peca';

export interface PecaDto {
  readonly id: number;
  readonly nome: string;
  readonly tipo: TipoPeca;
  readonly material: string | null;
  /** Milésimos (750 = 18k). */
  readonly teor: number | null;
  readonly pesoGramas: number | null;
  readonly ativo: boolean;
}

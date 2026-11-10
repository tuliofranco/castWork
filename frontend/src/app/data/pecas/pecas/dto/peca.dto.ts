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
  /**
   * Custo por unidade (R$), de compra ou produção. Só vem quando a consulta pede (`comCusto`, para quem tem
   * `precificacao.ler`); `null` = peça ainda sem custo.
   */
  readonly custo?: number | null;
}

/** Peça com os campos do formulário de edição. */
export interface PecaDetalheDto extends PecaDto {
  readonly descricao: string | null;
}

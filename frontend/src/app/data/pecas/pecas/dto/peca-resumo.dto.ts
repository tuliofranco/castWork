export type StatusPeca =
  | 'BRIEFING'
  | 'ORCAMENTO'
  | 'APROVADO'
  | 'PRODUCAO'
  | 'ACABAMENTO'
  | 'ENTREGUE'
  | 'POS_VENDA'
  | 'CANCELADA';

/** Linha da listagem de peças (campos pedidos na query GetPecas). */
export interface PecaResumoDto {
  readonly id: number;
  readonly titulo: string;
  readonly status: StatusPeca;
  /** Data sem hora, `YYYY-MM-DD`. */
  readonly prazoEntrega: string | null;
  readonly criadoEm: string;
  readonly cliente: { readonly id: number; readonly nome: string };
}

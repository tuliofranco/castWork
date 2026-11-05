export interface PostPecaDto {
  readonly clienteId: number;
  readonly titulo: string;
  readonly descricao?: string | null;
  /** `YYYY-MM-DD` */
  readonly prazoEntrega?: string | null;
}

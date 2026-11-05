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
  readonly pecaId: number;
  readonly quantidade: number;
  readonly observacao?: string | null;
}

export interface PutItemDto {
  readonly quantidade: number;
  readonly observacao?: string | null;
}

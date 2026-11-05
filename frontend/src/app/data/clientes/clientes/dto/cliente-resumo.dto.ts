export interface ClienteResumoDto {
  readonly id: number;
  readonly nome: string;
  /** CPF/CNPJ já com máscara. */
  readonly documento: string | null;
  readonly email: string | null;
  readonly telefone: string | null;
  readonly ativo: boolean;
  readonly pecas: readonly { readonly id: number }[];
}

/** Opção do seletor de cliente (formulário de peça). */
export interface ClienteOpcaoDto {
  readonly id: number;
  readonly nome: string;
}

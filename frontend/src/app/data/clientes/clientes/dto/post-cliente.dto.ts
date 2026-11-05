export interface PostClienteDto {
  readonly nome: string;
  readonly documento?: string | null;
  readonly email?: string | null;
  readonly telefone?: string | null;
}

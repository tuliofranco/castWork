/** Resposta 201 dos POST de cadastro: só o Id; a leitura completa vem pelo GraphQL. */
export interface IdCriadoDto {
  readonly id: number;
}

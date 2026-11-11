/** Uma peça que entrou no modelo da versão, com quantas unidades. */
export interface PecaMontadaDto {
  readonly nome: string;
  readonly quantidade: number;
}

/**
 * "Montar a partir das peças": o arquivo gravado, o de antes (nulo se a versão não tinha modelo; o histórico restaura),
 * as peças que entraram e as que ficaram de fora por não terem modelo 3D.
 */
export interface ModeloMontadoDto {
  readonly arquivoId: number;
  readonly arquivoAnteriorId: number | null;
  readonly pecas: readonly PecaMontadaDto[];
  readonly pecasSemModelo: readonly string[];
}

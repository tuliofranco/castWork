export interface PostLoginRespostaDto {
  /** JWT de acesso, de vida curta. */
  readonly accessToken: string;
  /** Token opaco usado para renovar o accessToken (fluxo da Etapa 4). */
  readonly refreshToken: string;
}

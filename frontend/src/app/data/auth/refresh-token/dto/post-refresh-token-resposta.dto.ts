export interface PostRefreshTokenRespostaDto {
  readonly accessToken: string;
  /** Rotação: cada refresh devolve um refresh token novo e invalida o anterior. */
  readonly refreshToken: string;
}

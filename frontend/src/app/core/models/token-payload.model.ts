/**
 * Claims que o Castwork espera no JWT. É o contrato com quem emite o token:
 * qualquer mudança aqui precisa ser combinada com o emissor.
 */
export interface TokenPayload {
  /** Id do usuário. */
  readonly sub: string;
  readonly nome: string;
  readonly email: string;
  /** Nome do ateliê da instalação (o sistema atende um único ateliê). */
  readonly atelieNome: string;
  /** Permissões `<dominio>.<acao>`. Alguns emissores mandam string única quando há só uma. */
  readonly permissoes: readonly string[] | string;
  /** Expiração em segundos desde a época Unix (claim padrão do JWT). */
  readonly exp: number;
}

import type { CanDeactivateFn } from '@angular/router';

/** Tela que pode ter trabalho ainda não salvo (Desenho 2D, Modelo 3D). */
export interface TelaComTrabalhoNaoSalvo {
  /** Falso quando o joalheiro prefere ficar para salvar. */
  podeSair(): Promise<boolean>;
}

/**
 * Antes de sair de uma ferramenta, pergunta se há trabalho não salvo. Sessão expirada (destino `/login`) sai direto:
 * salvar falharia do mesmo jeito.
 */
export const sairDaFerramentaGuard: CanDeactivateFn<TelaComTrabalhoNaoSalvo> = (tela, _rota, _atual, destino) =>
  destino.url.startsWith('/login') || tela.podeSair();

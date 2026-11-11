import type { CanMatchFn } from '@angular/router';

/**
 * Só casa a rota quando os parâmetros de Id (`:id`, `:versaoId`, `:pecaId`...) são inteiros positivos. Um endereço como
 * `/projetos/abc` deixa de casar e cai na página "não encontrada", em vez de consultar a API com `NaN`.
 */
export const idsNumericos: CanMatchFn = (rota, segmentos) =>
  (rota.path ?? '').split('/').every((parte, i) => !/^:(id|\w+Id)$/.test(parte) || /^[1-9]\d*$/.test(segmentos[i]?.path ?? ''));

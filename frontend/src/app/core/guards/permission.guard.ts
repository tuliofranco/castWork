import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';
import type { RotaData } from '@core/models/rota-data.model';
import { PermissaoService } from '@core/services/permissao.service';

/**
 * Lê `route.data.permissoes` e bloqueia (→ /403) quem não tem nenhuma delas.
 * Usado como `canActivateChild` no layout: protege todas as features com uma única declaração.
 *
 * Importante: isto é UX, não segurança. A API precisa validar a permissão de novo;
 * qualquer um pode editar o JS no navegador.
 */
export const permissionGuard: CanActivateFn = (route) => {
  const { permissoes = [] } = route.data as RotaData;
  return inject(PermissaoService).temAlguma(permissoes) || inject(Router).createUrlTree(['/403']);
};

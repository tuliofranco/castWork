import type { CastworkRoute } from '@core/models/rota-data.model';

import type { MenuItem } from './menu-item.model';

/** Predicado de permissão; na Etapa 3 será o `PermissaoService`. */
export type VerificadorPermissao = (permissoes: readonly string[]) => boolean;

const PERMITIR_TUDO: VerificadorPermissao = () => true;

/**
 * Função pura: transforma as rotas filhas do layout em itens de menu.
 * Mantida fora do componente para ser testada sem TestBed.
 */
export function montarMenu(rotas: readonly CastworkRoute[], temPermissao = PERMITIR_TUDO): MenuItem[] {
  return rotas
    .filter((rota) => rota.data?.menu === true && rota.path !== undefined)
    .filter((rota) => !rota.data?.permissoes?.length || temPermissao(rota.data.permissoes))
    .map((rota) => ({
      titulo: rota.data?.title ?? rota.path ?? '',
      caminho: `/${rota.path ?? ''}`,
      icone: rota.data?.icon ?? 'circle',
      ordem: rota.data?.order ?? Number.MAX_SAFE_INTEGER,
    }))
    .sort((a, b) => a.ordem - b.ordem);
}

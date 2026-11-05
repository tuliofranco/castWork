import type { HttpInterceptorFn } from '@angular/common/http';

import { erroGlobalInterceptor } from './erro-global.interceptor';
import { jwtInterceptor } from './jwt.interceptor';
import { loadingInterceptor } from './loading.interceptor';
import { removerParametrosNulosInterceptor } from './remover-parametros-nulos.interceptor';
import { serializarDatasInterceptor } from './serializar-datas.interceptor';
import { urlBaseInterceptor } from './url-base.interceptor';

/**
 * A ORDEM IMPORTA. A requisição desce esta lista de cima para baixo; a resposta sobe de baixo para cima.
 *
 * - urlBase primeiro: os demais já enxergam a URL absoluta (o jwt decide por ela se manda o token).
 * - nulos antes de datas: não adianta formatar o que vai ser removido.
 * - loading envolve todo o resto: o refresh + repetição contam como UMA operação para o usuário.
 * - erroGlobal ACIMA do jwt: um 401 resolvido pelo refresh nunca chega a virar toast.
 * - jwt por último: é o mais próximo da rede, então é ele que vê o 401 primeiro.
 */
export const INTERCEPTORS: HttpInterceptorFn[] = [
  urlBaseInterceptor,
  removerParametrosNulosInterceptor,
  serializarDatasInterceptor,
  loadingInterceptor,
  erroGlobalInterceptor,
  jwtInterceptor,
];

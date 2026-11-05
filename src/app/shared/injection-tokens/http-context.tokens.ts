import { HttpContext, HttpContextToken } from '@angular/common/http';

/**
 * Cada token desliga UM interceptor para UMA requisição. O valor padrão (false) mantém o comportamento normal.
 * Uso: `http.get(url, { context: semLoading() })` ou combinando: `semErro(semLoading())`.
 */
export const BYPASS_URL_BASE = new HttpContextToken<boolean>(() => false);
export const BYPASS_AUTH = new HttpContextToken<boolean>(() => false);
export const BYPASS_LOADING = new HttpContextToken<boolean>(() => false);
export const BYPASS_ERROR = new HttpContextToken<boolean>(() => false);

export const semUrlBase = (contexto = new HttpContext()) => contexto.set(BYPASS_URL_BASE, true);
export const semAuth = (contexto = new HttpContext()) => contexto.set(BYPASS_AUTH, true);
export const semLoading = (contexto = new HttpContext()) => contexto.set(BYPASS_LOADING, true);
export const semErro = (contexto = new HttpContext()) => contexto.set(BYPASS_ERROR, true);

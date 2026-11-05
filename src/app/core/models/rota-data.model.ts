import type { Route } from '@angular/router';

/**
 * Metadados que toda rota do Castwork pode declarar em `data`.
 * A rota é a fonte única para título da aba, menu lateral e permissões.
 */
export interface RotaData {
  /** Título da página; a TitleStrategy acrescenta o sufixo "| Castwork". */
  readonly title?: string;
  /** Quando true, a rota aparece no menu lateral. */
  readonly menu?: boolean;
  /** Posição no menu (crescente). */
  readonly order?: number;
  /** Classe do Bootstrap Icons, sem o prefixo `bi-` (ex.: `people`). */
  readonly icon?: string;
  /** Permissões no formato `<dominio>.<acao>`; basta ter uma delas. */
  readonly permissoes?: readonly string[];
}

/** `Route` com `data` tipado. Use no lugar de `Routes` para ter autocomplete e checagem. */
export type CastworkRoute = Omit<Route, 'data' | 'children'> & {
  /** Chaves extras (ex.: `codigo` da página de erro) são permitidas, mas as conhecidas continuam tipadas. */
  data?: RotaData & Readonly<Record<string, unknown>>;
  children?: CastworkRoute[];
};

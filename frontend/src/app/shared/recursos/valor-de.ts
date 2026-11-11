import type { Resource } from '@angular/core';

/**
 * O valor do recurso, ou `undefined` enquanto ele não tem valor (carregando pela primeira vez ou em erro).
 *
 * Ler `recurso.value()` direto lança quando o recurso está em erro: o template para no meio e a mensagem de erro com
 * "Tentar de novo" nunca aparece. Toda leitura de recurso passa por aqui.
 *
 * `null` continua sendo valor (por exemplo, "projeto sem briefing"): só `undefined` quer dizer "sem valor".
 */
export function valorDe<T>(recurso: Resource<T>): T | undefined {
  return recurso.hasValue() ? recurso.value() : undefined;
}

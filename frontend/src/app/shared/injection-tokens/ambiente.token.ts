import { InjectionToken } from '@angular/core';
import { environment } from '@env/environment';
import type { Environment } from '@env/environment.model';

/**
 * Configuração do ambiente via DI, em vez de importar `environment` direto:
 * nos testes basta `{ provide: AMBIENTE, useValue: {...} }` para trocar o comportamento.
 */
export const AMBIENTE = new InjectionToken<Environment>('AMBIENTE', {
  providedIn: 'root',
  factory: () => environment,
});

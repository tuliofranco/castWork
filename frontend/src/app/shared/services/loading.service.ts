import { Injectable, computed, signal } from '@angular/core';

/**
 * Contador de requisições em andamento. Contador (e não boolean) porque várias requisições
 * se sobrepõem: o loading só some quando a ÚLTIMA terminar.
 */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly emAndamento = signal(0);
  readonly ativo = computed(() => this.emAndamento() > 0);

  iniciar(): void {
    this.emAndamento.update((n) => n + 1);
  }

  encerrar(): void {
    this.emAndamento.update((n) => Math.max(0, n - 1));
  }
}

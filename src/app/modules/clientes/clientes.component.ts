import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Placeholder: vira o wrapper da feature (breadcrumb + router-outlet) nas próximas etapas. */
@Component({
  selector: 'cw-clientes',
  template: '<h1 class="h3">Clientes</h1>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientesComponent {}

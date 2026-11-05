import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Rodapé de formulário que gruda no fim da área de conteúdo: a ação principal fica ao alcance do polegar
 * no tablet. Coloque a ação principal por último (fica à direita).
 */
@Component({
  selector: 'cw-barra-acoes',
  template: '<ng-content />',
  styles: `
    :host {
      position: sticky;
      bottom: 0;
      z-index: 5;
      display: flex;
      flex-wrap: wrap;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-top: 2rem;
      padding: 1rem 0 1.25rem;
      background: linear-gradient(to bottom, transparent, var(--cw-bg) 1.25rem);
    }
    :host ::ng-deep .btn {
      min-height: var(--cw-alvo);
      min-width: 8.5rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarraAcoesComponent {}

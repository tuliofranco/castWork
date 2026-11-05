import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LoadingService } from '@shared/services/loading.service';
import { type TipoToast, ToastService } from '@shared/services/toast.service';

const CLASSE_POR_TIPO: Record<TipoToast, string> = {
  sucesso: 'text-bg-success',
  erro: 'text-bg-danger',
  aviso: 'text-bg-warning',
  info: 'text-bg-info',
};

/** Versão mínima para ver os interceptors funcionando; ganha acabamento na Etapa 7. */
@Component({
  selector: 'cw-toast-container',
  template: `
    @if (loading.ativo()) {
      <div class="progress position-fixed top-0 start-0 w-100 rounded-0" style="height: 3px; z-index: 1100" role="progressbar" aria-label="Carregando">
        <div class="progress-bar progress-bar-striped progress-bar-animated w-100"></div>
      </div>
    }
    <div class="toast-container position-fixed bottom-0 end-0 p-3" aria-live="polite">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast show align-items-center border-0" [class]="classes[toast.tipo]" role="status">
          <div class="d-flex">
            <div class="toast-body">{{ toast.mensagem }}</div>
            <button type="button" class="btn-close me-2 m-auto" aria-label="Fechar" (click)="toastService.remover(toast.id)"></button>
          </div>
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastContainerComponent {
  protected readonly toastService = inject(ToastService);
  protected readonly loading = inject(LoadingService);
  protected readonly classes = CLASSE_POR_TIPO;
}

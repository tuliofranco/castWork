import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LoadingService } from '@shared/services/loading.service';
import { type TipoToast, ToastService } from '@shared/services/toast.service';

const ICONE_POR_TIPO: Record<TipoToast, string> = {
  sucesso: 'check-circle-fill',
  erro: 'exclamation-octagon-fill',
  aviso: 'exclamation-triangle-fill',
  info: 'info-circle-fill',
};

/**
 * Avisos no canto inferior (acima da barra de navegação no celular) e barra fina de carregamento no topo.
 * Fundo neutro com ícone colorido: legível sob luz forte e sem gritar.
 */
@Component({
  selector: 'cw-toast-container',
  template: `
    @if (loading.ativo()) {
      <div class="carregando" role="progressbar" aria-label="Carregando"><span></span></div>
    }
    <div class="pilha" aria-live="polite">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast-cw" [class]="'tipo-' + toast.tipo" role="status">
          <i class="bi" [class]="'bi-' + icones[toast.tipo]" aria-hidden="true"></i>
          <span class="mensagem">{{ toast.mensagem }}</span>
          <button type="button" class="btn btn-sutil btn-icone" aria-label="Fechar aviso" (click)="toastService.remover(toast.id)">
            <i class="bi bi-x-lg" aria-hidden="true"></i>
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .carregando {
      position: fixed;
      inset: 0 0 auto;
      z-index: 1100;
      height: 3px;
      overflow: hidden;
    }
    .carregando span {
      display: block;
      width: 40%;
      height: 100%;
      background: var(--cw-accent);
      animation: correr 1.1s var(--cw-ease) infinite;
    }
    @keyframes correr {
      from { transform: translateX(-100%); }
      to { transform: translateX(250%); }
    }
    .pilha {
      position: fixed;
      right: 1rem;
      bottom: calc(1rem + env(safe-area-inset-bottom));
      z-index: 1090;
      display: grid;
      gap: 0.5rem;
      width: min(24rem, calc(100vw - 2rem));
    }
    @media (max-width: 767.98px) {
      .pilha { bottom: calc(var(--cw-barra-inferior) + 1rem + env(safe-area-inset-bottom)); }
    }
    .toast-cw {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.375rem 0.375rem 0.375rem 1rem;
      border: 1px solid var(--cw-border);
      border-radius: var(--cw-radius-lg);
      background: var(--cw-surface);
      color: var(--cw-ink);
      box-shadow: var(--cw-shadow-pop);
      animation: entrar var(--cw-rapido) var(--cw-ease);
    }
    @keyframes entrar {
      from { opacity: 0; transform: translateY(6px); }
    }
    .mensagem { flex: 1 1 auto; font-weight: 500; }
    .tipo-sucesso > .bi { color: var(--cw-success); }
    .tipo-erro > .bi { color: var(--cw-danger); }
    .tipo-aviso > .bi { color: var(--cw-warning); }
    .tipo-info > .bi { color: var(--cw-accent); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastContainerComponent {
  protected readonly toastService = inject(ToastService);
  protected readonly loading = inject(LoadingService);
  protected readonly icones = ICONE_POR_TIPO;
}

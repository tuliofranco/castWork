import { semErro } from '@shared/injection-tokens/http-context.tokens';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, effect, inject, input, signal, viewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';

/**
 * Imagem de uma conversa com IA, em miniatura que amplia ao tocar. `src` é um caminho da API (lido pelo
 * HttpClient, com o token: um `<img src>` direto não leva o cabeçalho de autenticação) ou uma URL local
 * (`data:`/`blob:`, a imagem que acabou de ser anexada).
 */
@Component({
  selector: 'cw-imagem-protegida',
  template: `
    <button type="button" class="miniatura" [attr.aria-label]="falhou() ? 'Imagem indisponível: ' + rotulo() + '. Tentar de novo' : 'Ampliar ' + rotulo()" [disabled]="!url() && !falhou()" (click)="falhou() ? tentarDeNovo() : ampliar()">
      @if (url(); as u) {
        <img [src]="u" [alt]="rotulo()" />
      } @else if (falhou()) {
        <span class="carregando" aria-hidden="true" title="Imagem indisponível"><i class="bi bi-arrow-clockwise"></i></span>
      } @else {
        <span class="carregando" aria-hidden="true"><i class="bi bi-image"></i></span>
      }
    </button>
    <dialog #ampliada class="ampliada" [attr.aria-label]="rotulo()">
      <button type="button" class="fechar" aria-label="Fechar" (click)="ampliada.close()"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
      @if (url(); as u) {
        <img [src]="u" [alt]="rotulo()" />
      }
    </dialog>
  `,
  styles: `
    .miniatura {
      display: block;
      padding: 0;
      border: 1px solid var(--cw-border);
      border-radius: var(--cw-radius);
      overflow: hidden;
      background: var(--cw-surface-2);
    }

    img {
      display: block;
      width: 9rem;
      height: 9rem;
      object-fit: cover;
    }

    .carregando {
      display: grid;
      place-items: center;
      width: 9rem;
      height: 9rem;
      color: var(--cw-ink-muted);
      font-size: 1.5rem;
    }

    .ampliada {
      max-width: 94vw;
      max-height: 94dvh;
      padding: 0;
      border: 0;
      background: transparent;

      &::backdrop {
        background: oklch(0.12 0.01 60 / 0.85);
      }

      .fechar {
        position: fixed;
        top: 1rem;
        right: 1rem;
        display: grid;
        place-items: center;
        width: var(--cw-alvo);
        height: var(--cw-alvo);
        border: 0;
        border-radius: 999px;
        background: var(--cw-surface);
        color: var(--cw-ink);
      }

      img {
        width: auto;
        height: auto;
        max-width: 94vw;
        max-height: 94dvh;
        object-fit: contain;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImagemProtegidaComponent {
  private readonly http = inject(HttpClient);

  readonly src = input.required<string>();
  readonly rotulo = input('Imagem enviada');

  protected readonly url = signal<string | null>(null);
  /** A imagem não veio (rede, 404, 500): a miniatura vira um "tentar de novo". */
  protected readonly falhou = signal(false);
  private readonly tentativa = signal(0);
  private readonly ampliadaRef = viewChild.required<ElementRef<HTMLDialogElement>>('ampliada');

  constructor() {
    effect((onCleanup) => {
      const src = this.src();
      this.tentativa();
      this.falhou.set(false);
      if (/^(data|blob):/.test(src)) {
        this.url.set(src);
        return;
      }

      // A imagem anterior sai de cena (e da memória) quando o `src` muda.
      this.url.set(null);
      let criada: string | null = null;
      const assinatura = this.http.get(src, { responseType: 'blob', context: semErro() }).subscribe({
        next: (blob) => {
          criada = URL.createObjectURL(blob);
          this.url.set(criada);
        },
        // Sem isto a miniatura ficava "carregando" para sempre.
        error: () => this.falhou.set(true),
      });
      onCleanup(() => {
        assinatura.unsubscribe();
        if (criada) URL.revokeObjectURL(criada);
      });
    });
    inject(DestroyRef).onDestroy(() => {
      const url = this.url();
      if (url?.startsWith('blob:') && !this.src().startsWith('blob:')) URL.revokeObjectURL(url);
    });
  }

  protected tentarDeNovo(): void {
    this.tentativa.update((n) => n + 1);
  }

  protected ampliar(): void {
    this.ampliadaRef().nativeElement.showModal();
  }
}

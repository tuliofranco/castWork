import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FerramentasService } from '@data/projetos/ferramentas/ferramentas.service';

/**
 * O desenho 2D da versão (o que a IA e o joalheiro fizeram no CADCraft), como imagem na página da versão:
 * não precisa abrir o editor para ver a peça. Não aparece enquanto a versão não tem desenho.
 */
@Component({
  selector: 'cw-previa-desenho',
  imports: [RouterLink],
  template: `
    @if (url(); as u) {
      <section class="previa" aria-labelledby="titulo-previa">
        <header>
          <h2 id="titulo-previa">Desenho 2D</h2>
          <a class="btn btn-sutil" routerLink="desenho-2d"><i class="bi bi-vector-pen" aria-hidden="true"></i> Abrir no editor</a>
        </header>
        <button type="button" class="folha" aria-label="Ampliar o desenho" (click)="ampliada.showModal()">
          <img [src]="u" alt="Desenho 2D da versão" />
        </button>
        <dialog #ampliada class="ampliada" aria-label="Desenho 2D da versão">
          <button type="button" class="fechar" aria-label="Fechar" (click)="ampliada.close()"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
          <img [src]="u" alt="Desenho 2D da versão" />
        </dialog>
      </section>
    }
  `,
  styles: `
    .previa {
      margin-bottom: 2rem;

      header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        margin-bottom: 0.75rem;
      }

      h2 {
        margin: 0;
        font-size: 1.0625rem;
        font-weight: 600;
      }
    }

    /* O SVG do CADCraft vem com linhas pretas em fundo branco: a folha fica branca nos dois temas. */
    .folha {
      display: block;
      width: 100%;
      padding: 1rem;
      border: 1px solid var(--cw-border);
      border-radius: var(--cw-radius-lg);
      background: #fff;
      cursor: zoom-in;

      img {
        display: block;
        width: 100%;
        height: 22rem;
        object-fit: contain;
      }
    }

    .ampliada {
      width: 94vw;
      max-width: 94vw;
      height: 94dvh;
      max-height: 94dvh;
      padding: 1rem;
      border: 0;
      border-radius: var(--cw-radius-lg);
      background: #fff;

      &::backdrop {
        background: oklch(0.12 0.01 60 / 0.85);
      }

      img {
        width: 100%;
        height: 100%;
        object-fit: contain;
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
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PreviaDesenhoComponent {
  private readonly ferramentas = inject(FerramentasService);

  readonly projetoId = input.required<number>();
  readonly versaoId = input.required<number>();

  protected readonly url = signal<string | null>(null);

  constructor() {
    effect((onCleanup) => {
      let ativo = true;
      onCleanup(() => (ativo = false));
      this.ferramentas.getPrevia(this.projetoId(), this.versaoId(), 'desenho-2d').subscribe({
        next: (svg) => ativo && this.trocar(svg ? URL.createObjectURL(svg) : null),
        // Sem prévia (CADCraft fora do ar, por exemplo) a página segue sem o desenho.
        error: () => undefined,
      });
    });
    inject(DestroyRef).onDestroy(() => this.trocar(null));
  }

  private trocar(url: string | null): void {
    const anterior = this.url();
    if (anterior) URL.revokeObjectURL(anterior);
    this.url.set(url);
  }
}

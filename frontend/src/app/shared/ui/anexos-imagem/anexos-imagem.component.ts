import { ChangeDetectionStrategy, Component, ElementRef, inject, input, model, viewChild } from '@angular/core';
import { ToastService } from '@shared/services/toast.service';

import { ehHeic, type ImagemAnexada, prepararImagem } from './preparar-imagem';

/**
 * Imagens anexadas a uma mensagem antes de enviar: miniaturas com remover, e o seletor de arquivos
 * (no tablet, câmera ou galeria). O botão de anexar fica no compositor e chama `escolher()`;
 * colar e arrastar chegam por `adicionar()`.
 */
@Component({
  selector: 'cw-anexos-imagem',
  template: `
    <input #seletor type="file" accept="image/png,image/jpeg,image/webp,image/heic,image/heif,.heic,.heif,image/*" multiple hidden (change)="aoEscolher($event)" />
    @if (imagens().length > 0) {
      <ul class="miniaturas">
        @for (imagem of imagens(); track imagem.url; let i = $index) {
          <li>
            <img [src]="imagem.url" [alt]="'Imagem ' + (i + 1)" />
            <button type="button" class="remover" [attr.aria-label]="'Remover imagem ' + (i + 1)" (click)="remover(i)">
              <i class="bi bi-x" aria-hidden="true"></i>
            </button>
          </li>
        }
      </ul>
    }
  `,
  styles: `
    :host {
      display: block;
    }

    .miniaturas {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin: 0 0 0.5rem;
      padding: 0;
      list-style: none;
    }

    li {
      position: relative;
    }

    img {
      display: block;
      width: 4rem;
      height: 4rem;
      border: 1px solid var(--cw-border);
      border-radius: var(--cw-radius);
      object-fit: cover;
    }

    .remover {
      position: absolute;
      top: -0.5rem;
      right: -0.5rem;
      display: grid;
      place-items: center;
      width: 1.75rem;
      height: 1.75rem;
      padding: 0;
      border: 1px solid var(--cw-border);
      border-radius: 999px;
      background: var(--cw-surface);
      color: var(--cw-ink);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AnexosImagemComponent {
  private readonly toast = inject(ToastService);

  readonly imagens = model<readonly ImagemAnexada[]>([]);
  readonly maximo = input(4);

  private readonly seletor = viewChild.required<ElementRef<HTMLInputElement>>('seletor');

  escolher(): void {
    this.seletor().nativeElement.click();
  }

  /** Arquivos vindos do seletor, de colar (Ctrl+V) ou de arrastar. Ignora o que não for imagem. */
  async adicionar(arquivos: Iterable<File>): Promise<void> {
    const imagens = [...arquivos].filter((a) => a.type.startsWith('image/') || ehHeic(a));
    const livres = this.maximo() - this.imagens().length;
    if (imagens.length > livres) {
      this.toast.erro(`Até ${this.maximo()} imagens por mensagem.`);
    }

    for (const arquivo of imagens.slice(0, Math.max(0, livres))) {
      try {
        const imagem = await prepararImagem(arquivo);
        // O espaço livre é conferido de novo aqui: outra leva (colar e arrastar juntos) pode ter entrado durante o preparo.
        this.imagens.update((lista) => (lista.length < this.maximo() ? [...lista, imagem] : lista));
      } catch {
        this.toast.erro(`Não foi possível abrir ${arquivo.name}.`);
      }
    }
  }

  /** Para o `(paste)` do campo de texto: imagens coladas viram anexos; texto colado segue normal. */
  colar(evento: ClipboardEvent): void {
    const arquivos = [...(evento.clipboardData?.files ?? [])];
    if (arquivos.some((a) => a.type.startsWith('image/') || ehHeic(a))) {
      evento.preventDefault();
      void this.adicionar(arquivos);
    }
  }

  protected aoEscolher(evento: Event): void {
    const campo = evento.target as HTMLInputElement;
    void this.adicionar(campo.files ?? []);
    campo.value = '';
  }

  protected remover(indice: number): void {
    this.imagens.update((lista) => lista.filter((_, i) => i !== indice));
  }
}

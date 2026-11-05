import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class DownloadService {
  private readonly document = inject(DOCUMENT);

  /** Salva o Blob como arquivo: cria uma URL temporária, clica num <a download> e libera a memória. */
  salvar(arquivo: Blob, nome: string): void {
    const url = URL.createObjectURL(arquivo);
    const link = this.document.createElement('a');
    link.href = url;
    link.download = nome;
    link.click();
    // Liberar no próximo ciclo: alguns navegadores ainda leem a URL logo após o click.
    setTimeout(() => URL.revokeObjectURL(url));
  }

  /** Abre o PDF numa nova aba (visualizador do navegador). */
  abrir(arquivo: Blob): void {
    const url = URL.createObjectURL(arquivo);
    this.document.defaultView?.open(url, '_blank', 'noopener');
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }
}

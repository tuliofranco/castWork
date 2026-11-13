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
    // Fora do documento, alguns navegadores ignoram o clique.
    this.document.body.appendChild(link);
    link.click();
    link.remove();
    // Com folga: o navegador pode ainda estar lendo a URL (arquivo grande, diálogo de "salvar como").
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }
}

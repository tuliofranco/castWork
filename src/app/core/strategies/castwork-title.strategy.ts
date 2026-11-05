import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { type ActivatedRouteSnapshot, type RouterStateSnapshot, TitleStrategy } from '@angular/router';

export const NOME_APLICACAO = 'Castwork';

/**
 * Lê `data.title` da rota ativa mais profunda e define o título da aba como "<título> | Castwork".
 * Sem título declarado, usa apenas "Castwork".
 */
@Injectable()
export class CastworkTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const titulo = this.tituloMaisProfundo(snapshot);
    this.title.setTitle(titulo ? `${titulo} | ${NOME_APLICACAO}` : NOME_APLICACAO);
  }

  private tituloMaisProfundo(snapshot: RouterStateSnapshot): string | undefined {
    let rota: ActivatedRouteSnapshot | null = snapshot.root;
    let titulo: string | undefined;
    while (rota) {
      const atual: unknown = rota.data['title'];
      if (typeof atual === 'string') titulo = atual;
      rota = rota.firstChild;
    }
    return titulo;
  }
}

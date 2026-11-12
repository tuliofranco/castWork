import { signal } from '@angular/core';

/**
 * Uma troca de arquivo por vez, nas duas ferramentas (Desenho 2D e Modelo 3D).
 *
 * O arquivo novo abre num segundo editor, por trás, e só depois troca de lugar com o que está à vista. Duas trocas ao
 * mesmo tempo disputariam o mesmo iframe e acabariam com os dois vazios; por isso um pedido que chega durante uma troca
 * não abre outra: a que está em andamento roda mais uma vez ao terminar, já com o arquivo mais novo.
 */
export class FilaDeTrocas {
  /** Há uma troca em andamento (a tela avisa, e o que grava ou troca o arquivo espera). */
  readonly trocando = signal(false);
  private emAndamento: Promise<void> | null = null;
  private pendente = false;

  /**
   * @param antes As trocas esperam por isto (a primeira abertura), para não descarregar o editor que ainda está subindo.
   * @param trocar Faz uma troca. Não deve rejeitar: a falha vira estado na tela de quem chamou.
   */
  constructor(
    private readonly antes: Promise<void>,
    private readonly trocar: () => Promise<void>,
  ) {}

  /** A troca em andamento, para quem precisa esperar por ela (nulo sem troca). */
  get atual(): Promise<void> | null {
    return this.emAndamento;
  }

  pedir(): Promise<void> {
    if (this.emAndamento) {
      this.pendente = true;
      return this.emAndamento;
    }
    this.trocando.set(true);
    this.emAndamento = (async () => {
      try {
        await this.antes;
        do {
          this.pendente = false;
          await this.trocar();
        } while (this.pendente);
      } finally {
        this.emAndamento = null;
        this.trocando.set(false);
      }
    })();
    return this.emAndamento;
  }
}

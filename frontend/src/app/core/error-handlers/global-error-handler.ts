import { HttpErrorResponse } from '@angular/common/http';
import { type ErrorHandler, Injectable, inject } from '@angular/core';
import type { NavigationError } from '@angular/router';
import { ToastService } from '@shared/services/toast.service';

/** O arquivo de uma tela (chunk lazy) não veio: quase sempre o app foi atualizado e o nome do arquivo mudou. */
export function ehFalhaAoBaixarTela(erro: unknown): boolean {
  const mensagem = erro instanceof Error ? `${erro.name} ${erro.message}` : String(erro);
  return /ChunkLoadError|Failed to fetch dynamically imported module|error loading dynamically imported module|Importing a module script failed/i.test(mensagem);
}

const MENSAGEM_TELA = 'Não foi possível abrir esta página. O Castwork pode ter sido atualizado: recarregue para continuar.';

/**
 * Último recurso para erros que ninguém tratou: registra no console e avisa, em vez de a tela simplesmente parar de
 * responder. Erros HTTP ficam de fora: o `erroGlobalInterceptor` já avisou.
 */
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly toast = inject(ToastService);

  handleError(erro: unknown): void {
    console.error(erro);
    if (erro instanceof HttpErrorResponse) return;
    this.toast.erro(ehFalhaAoBaixarTela(erro) ? MENSAGEM_TELA : 'Algo deu errado nesta tela. Se continuar, recarregue a página.');
  }
}

/** Navegação que falha (toque no menu sem efeito): avisa. Usado com `withNavigationErrorHandler`. */
export function avisarErroDeNavegacao(erro: NavigationError): void {
  const toast = inject(ToastService);
  console.error(erro.error);
  toast.erro(ehFalhaAoBaixarTela(erro.error) ? MENSAGEM_TELA : 'Não foi possível abrir esta página.');
}

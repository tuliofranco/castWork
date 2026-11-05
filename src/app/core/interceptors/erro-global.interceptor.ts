import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BYPASS_ERROR } from '@shared/injection-tokens/http-context.tokens';
import { ToastService } from '@shared/services/toast.service';
import { catchError, throwError } from 'rxjs';

/** Corpo de erro no padrão RFC 9457 (ProblemDetails), o padrão do ASP.NET Core. */
interface ProblemDetails {
  readonly title?: string;
  readonly detail?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

const MENSAGENS_POR_STATUS: Readonly<Record<number, string>> = {
  0: 'Sem conexão com o servidor. Verifique sua internet.',
  401: 'Sua sessão expirou. Entre novamente.',
  403: 'Você não tem permissão para esta ação.',
  404: 'Registro não encontrado.',
  409: 'Este registro foi alterado por outra pessoa. Recarregue e tente de novo.',
};

function ehProblemDetails(corpo: unknown): corpo is ProblemDetails {
  return typeof corpo === 'object' && corpo !== null && ('title' in corpo || 'detail' in corpo || 'errors' in corpo);
}

export function mensagemDeErroHttp(erro: HttpErrorResponse): string {
  const corpo: unknown = erro.error;
  // 400/422: a mensagem de validação da API é mais útil que qualquer texto genérico.
  if ((erro.status === 400 || erro.status === 422) && ehProblemDetails(corpo)) {
    const primeiraValidacao = Object.values(corpo.errors ?? {})[0]?.[0];
    return primeiraValidacao ?? corpo.detail ?? corpo.title ?? 'Dados inválidos.';
  }
  return MENSAGENS_POR_STATUS[erro.status] ?? (erro.status >= 500 ? 'Erro no servidor. Tente novamente em instantes.' : 'Não foi possível concluir a requisição.');
}

/**
 * 5. Toast padrão para qualquer erro HTTP. SEMPRE relança: quem chamou ainda pode reagir
 * (parar um spinner, reabrir um formulário). O toast é aviso, não tratamento.
 */
export const erroGlobalInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((erro: unknown) => {
      // Erro de download (responseType 'blob') chega com o corpo em Blob: quem baixou lê e mostra.
      const ignorar = req.context.get(BYPASS_ERROR) || !(erro instanceof HttpErrorResponse) || erro.error instanceof Blob;
      if (!ignorar) toast.erro(mensagemDeErroHttp(erro));
      return throwError(() => erro);
    }),
  );
};

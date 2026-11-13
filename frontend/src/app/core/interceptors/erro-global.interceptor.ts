import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BYPASS_ERROR } from '@shared/injection-tokens/http-context.tokens';
import { ToastService } from '@shared/services/toast.service';
import { erroLegivel } from '@shared/operators/ler-erro-blob.operator';
import { catchError, from, switchMap, throwError } from 'rxjs';

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
  429: 'Muitas tentativas em pouco tempo. Aguarde um instante e tente de novo.',
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
  // 403, 404 e 409: quando a API explica o motivo (regra de negócio), a explicação dela vale mais que o texto genérico.
  if ([403, 404, 409].includes(erro.status) && ehProblemDetails(corpo) && corpo.detail) return corpo.detail;
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
      if (req.context.get(BYPASS_ERROR) || !(erro instanceof HttpErrorResponse)) return throwError(() => erro);
      // Num download (responseType 'blob') o corpo do erro também chega como Blob: lê o ProblemDetails de dentro dele
      // antes de avisar, e relança o erro já convertido. Sem isso, a falha de um download passava em silêncio.
      if (!(erro.error instanceof Blob)) {
        toast.erro(mensagemDeErroHttp(erro));
        return throwError(() => erro);
      }
      return from(erroLegivel(erro)).pipe(
        switchMap((legivel) => {
          toast.erro(mensagemDeErroHttp(legivel));
          return throwError(() => legivel);
        }),
      );
    }),
  );
};

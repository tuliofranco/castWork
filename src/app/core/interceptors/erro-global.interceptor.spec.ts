import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { semErro } from '@shared/injection-tokens/http-context.tokens';
import { ToastService } from '@shared/services/toast.service';

import { erroGlobalInterceptor, mensagemDeErroHttp } from './erro-global.interceptor';
import { configurarHttp } from './testing';

describe('erroGlobalInterceptor', () => {
  const toast = { erro: vi.fn() };

  beforeEach(() => toast.erro.mockReset());

  function falhar(corpo: Blob | object | null, status: number, opcoes: Parameters<HttpClient['get']>[1] = {}): unknown {
    const http = configurarHttp([erroGlobalInterceptor], [{ provide: ToastService, useValue: toast }]);
    let recebido: unknown;
    TestBed.inject(HttpClient).get('x', opcoes).subscribe({ error: (e: unknown) => (recebido = e) });
    http.expectOne('x').flush(corpo, { status, statusText: 'Erro' });
    return recebido;
  }

  it('deve mostrar o toast E relançar o erro para quem chamou', () => {
    const erro = falhar(null, 500);
    expect(toast.erro).toHaveBeenCalledWith('Erro no servidor. Tente novamente em instantes.');
    expect(erro).toBeInstanceOf(HttpErrorResponse);
  });

  it('deve usar a mensagem de validação do ProblemDetails no 400', () => {
    falhar({ title: 'Validação', errors: { Nome: ['O nome é obrigatório.'] } }, 400);
    expect(toast.erro).toHaveBeenCalledWith('O nome é obrigatório.');
  });

  it('não deve mostrar toast com BYPASS_ERROR', () => {
    falhar(null, 500, { context: semErro() });
    expect(toast.erro).not.toHaveBeenCalled();
  });

  it('não deve mostrar toast quando o erro vem como Blob (download)', () => {
    falhar(new Blob(['{}']), 404, { responseType: 'blob' as 'json' });
    expect(toast.erro).not.toHaveBeenCalled();
  });

  it.each([
    [0, 'Sem conexão com o servidor. Verifique sua internet.'],
    [403, 'Você não tem permissão para esta ação.'],
    [404, 'Registro não encontrado.'],
    [409, 'Este registro foi alterado por outra pessoa. Recarregue e tente de novo.'],
    [418, 'Não foi possível concluir a requisição.'],
  ])('status %i → "%s"', (status, mensagem) => {
    expect(mensagemDeErroHttp(new HttpErrorResponse({ status }))).toBe(mensagem);
  });
});

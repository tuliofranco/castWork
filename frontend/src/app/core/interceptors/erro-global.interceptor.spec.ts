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

  it('deve ler o ProblemDetails de dentro do Blob e avisar quando um download falha', async () => {
    const http = configurarHttp([erroGlobalInterceptor], [{ provide: ToastService, useValue: toast }]);
    let recebido: unknown;
    TestBed.inject(HttpClient)
      .get('x', { responseType: 'blob' })
      .subscribe({ error: (e: unknown) => (recebido = e) });
    http.expectOne('x').flush(new Blob([JSON.stringify({ title: 'Conflito', detail: 'A peça ainda não tem desenho.' })]), { status: 409, statusText: 'Erro' });

    await vi.waitFor(() => expect(recebido).toBeInstanceOf(HttpErrorResponse));
    expect(toast.erro).toHaveBeenCalledWith('A peça ainda não tem desenho.');
    expect((recebido as HttpErrorResponse).error).toEqual({ title: 'Conflito', detail: 'A peça ainda não tem desenho.' });
  });

  it('deve preferir a explicação da API no 403, 404 e 409, e ter mensagem própria no 429', () => {
    expect(mensagemDeErroHttp(new HttpErrorResponse({ status: 404, error: { detail: 'Versão arquivada.' } }))).toBe('Versão arquivada.');
    expect(mensagemDeErroHttp(new HttpErrorResponse({ status: 429 }))).toBe('Muitas tentativas em pouco tempo. Aguarde um instante e tente de novo.');
    expect(mensagemDeErroHttp(new HttpErrorResponse({ status: 500, error: { detail: 'NullReferenceException' } }))).toBe('Erro no servidor. Tente novamente em instantes.');
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

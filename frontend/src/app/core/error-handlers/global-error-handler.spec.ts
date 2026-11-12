import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '@shared/services/toast.service';

import { GlobalErrorHandler, ehFalhaAoBaixarTela } from './global-error-handler';

describe('GlobalErrorHandler', () => {
  const toast = { erro: vi.fn() };
  let handler: GlobalErrorHandler;

  beforeEach(() => {
    toast.erro.mockReset();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    TestBed.configureTestingModule({ providers: [GlobalErrorHandler, { provide: ToastService, useValue: toast }] });
    handler = TestBed.inject(GlobalErrorHandler);
  });

  afterEach(() => vi.restoreAllMocks());

  it('deve avisar um erro que ninguém tratou', () => {
    handler.handleError(new TypeError('x is undefined'));
    expect(toast.erro).toHaveBeenCalledWith('Algo deu errado nesta tela. Se continuar, recarregue a página.');
  });

  it('deve pedir para recarregar quando o arquivo da tela não vem', () => {
    handler.handleError(new TypeError('Failed to fetch dynamically imported module: https://app/chunk-ABC.js'));
    expect(toast.erro).toHaveBeenCalledWith(expect.stringContaining('recarregue para continuar'));
  });

  it('não deve avisar de novo um erro HTTP (o interceptor já avisou)', () => {
    handler.handleError(new HttpErrorResponse({ status: 500 }));
    expect(toast.erro).not.toHaveBeenCalled();
  });

  it('deve reconhecer as mensagens de falha de import dinâmico dos navegadores', () => {
    expect(ehFalhaAoBaixarTela(new Error('error loading dynamically imported module'))).toBe(true);
    expect(ehFalhaAoBaixarTela(new Error('Importing a module script failed.'))).toBe(true);
    expect(ehFalhaAoBaixarTela(new Error('outro erro'))).toBe(false);
  });
});

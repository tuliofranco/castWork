import { CurrencyPipe, DatePipe } from '@angular/common';
import { DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { appConfig } from '../app.config';

describe('Locale pt-BR', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [...appConfig.providers, CurrencyPipe, DatePipe] });
  });

  it('deve usar pt-BR e BRL como padrão', () => {
    expect(TestBed.inject(LOCALE_ID)).toBe('pt-BR');
    expect(TestBed.inject(DEFAULT_CURRENCY_CODE)).toBe('BRL');
  });

  it('deve formatar moeda no padrão brasileiro', () => {
    const valor = TestBed.inject(CurrencyPipe).transform(1234.5);
    expect(valor?.replace(/\s/g, ' ')).toBe('R$ 1.234,50');
  });

  it('deve formatar data como dd/MM/yyyy', () => {
    expect(TestBed.inject(DatePipe).transform(new Date(2026, 10, 5), 'shortDate')).toBe('05/11/2026');
  });
});

import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import {
  ApplicationConfig,
  ErrorHandler,
  DEFAULT_CURRENCY_CODE,
  LOCALE_ID,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter, TitleStrategy, withComponentInputBinding, withNavigationErrorHandler } from '@angular/router';
import { avisarErroDeNavegacao, GlobalErrorHandler } from '@core/error-handlers/global-error-handler';
import { apolloFactory } from '@core/factories/apollo.factory';
import { INTERCEPTORS } from '@core/interceptors';
import { CastworkTitleStrategy } from '@core/strategies/castwork-title.strategy';

import { provideApollo } from 'apollo-angular';

import { routes } from './app.routes';

// Registra os dados de formatação (datas, números, moeda) do pt-BR para os pipes do Angular.
registerLocaleData(localePt, 'pt-BR');

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideRouter(routes, withComponentInputBinding(), withNavigationErrorHandler(avisarErroDeNavegacao)),
    provideHttpClient(withFetch(), withInterceptors(INTERCEPTORS)),
    provideApollo(apolloFactory),
    { provide: LOCALE_ID, useValue: 'pt-BR' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'BRL' },
    { provide: TitleStrategy, useClass: CastworkTitleStrategy },
  ],
};

import { type HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import type { EnvironmentProviders, Provider } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import type { Environment } from '@env/environment.model';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';

export const AMBIENTE_TESTE: Environment = {
  production: false,
  apiUrl: 'https://api.teste/api',
  graphqlUrl: 'https://api.teste/graphql',
  usarMocks: false,
};

/** Configura HttpClient com só os interceptors do teste e devolve o controller. */
export function configurarHttp(
  interceptors: HttpInterceptorFn[],
  providers: (Provider | EnvironmentProviders)[] = [],
): HttpTestingController {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors(interceptors)),
      provideHttpClientTesting(),
      { provide: AMBIENTE, useValue: AMBIENTE_TESTE },
      ...providers,
    ],
  });
  return TestBed.inject(HttpTestingController);
}

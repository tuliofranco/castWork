import { ApplicationRef, Injector, runInInjectionContext } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TestBed } from '@angular/core/testing';
import { type Observable, of, throwError } from 'rxjs';

import { valorDe } from './valor-de';

describe('valorDe', () => {
  async function recurso<T>(stream: () => Observable<T>) {
    const r = runInInjectionContext(TestBed.inject(Injector), () => rxResource({ stream }));
    await TestBed.inject(ApplicationRef).whenStable();
    return r;
  }

  it('deve devolver o valor carregado', async () => {
    expect(valorDe(await recurso(() => of([1, 2])))).toEqual([1, 2]);
  });

  it('deve manter null como valor', async () => {
    expect(valorDe(await recurso(() => of(null)))).toBeNull();
  });

  it('deve devolver undefined, sem lançar, com o recurso em erro', async () => {
    const r = await recurso(() => throwError(() => new Error('API fora')));

    expect(r.error()).toBeDefined();
    expect(() => r.value()).toThrow();
    expect(valorDe(r)).toBeUndefined();
  });
});

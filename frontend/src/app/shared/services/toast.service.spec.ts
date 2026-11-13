import { TestBed } from '@angular/core/testing';

import { ToastService } from './toast.service';

describe('ToastService', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('deve adicionar e remover sozinho após 5 s', () => {
    const service = TestBed.inject(ToastService);
    service.sucesso('Salvo');
    expect(service.toasts()).toEqual([{ id: 1, tipo: 'sucesso', mensagem: 'Salvo' }]);
    vi.advanceTimersByTime(5000);
    expect(service.toasts()).toEqual([]);
  });

  it('não deve duplicar a mesma mensagem ativa', () => {
    const service = TestBed.inject(ToastService);
    service.erro('Servidor fora');
    service.erro('Servidor fora');
    service.aviso('Servidor fora');
    expect(service.toasts()).toHaveLength(2);
  });

  it('deve manter o erro por 10 s', () => {
    const service = TestBed.inject(ToastService);
    service.erro('Servidor fora');
    vi.advanceTimersByTime(9999);
    expect(service.toasts()).toHaveLength(1);
    vi.advanceTimersByTime(1);
    expect(service.toasts()).toEqual([]);
  });

  it('deve renovar o tempo quando a mesma mensagem chega de novo', () => {
    const service = TestBed.inject(ToastService);
    service.sucesso('Salvo');
    vi.advanceTimersByTime(4000);
    service.sucesso('Salvo');
    vi.advanceTimersByTime(4000);
    expect(service.toasts()).toHaveLength(1);
    vi.advanceTimersByTime(1000);
    expect(service.toasts()).toEqual([]);
  });
});

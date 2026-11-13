import { TestBed } from '@angular/core/testing';

import { QuantidadeComponent } from './quantidade.component';

describe('QuantidadeComponent', () => {
  async function criar(valor = 3) {
    const fixture = TestBed.createComponent(QuantidadeComponent);
    fixture.componentRef.setInput('valor', valor);
    fixture.componentRef.setInput('rotulo', 'Elo');
    const emitidos: number[] = [];
    fixture.componentInstance.alterado.subscribe((n) => emitidos.push(n));
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    return { fixture, el, emitidos, mais: el.querySelector<HTMLButtonElement>('button[aria-label="Aumentar Elo"]')!, campo: el.querySelector<HTMLInputElement>('input')! };
  }

  afterEach(() => vi.useRealTimers());

  it('deve emitir uma vez só, depois do tempo de espera, a soma dos toques', async () => {
    const { emitidos, mais } = await criar();
    vi.useFakeTimers();

    mais.click();
    mais.click();
    expect(emitidos).toEqual([]);
    vi.advanceTimersByTime(500);

    expect(emitidos).toEqual([5]);
  });

  it('deve gravar na hora ao sair do controle antes do tempo de espera', async () => {
    const { el, emitidos, mais } = await criar();
    vi.useFakeTimers();

    mais.click();
    el.dispatchEvent(new FocusEvent('focusout', { relatedTarget: document.body }));

    expect(emitidos).toEqual([4]);
    vi.advanceTimersByTime(500);
    expect(emitidos).toEqual([4]);
  });

  it('deve voltar ao valor salvo quando a gravação falha, mesmo com o valor igual', async () => {
    const { fixture, emitidos, mais } = await criar();
    vi.useFakeTimers();
    mais.click();
    vi.advanceTimersByTime(500);
    expect(emitidos).toEqual([4]);
    vi.useRealTimers();

    // O pai não mudou o `valor` (o servidor recusou): só avisa a falha.
    fixture.componentRef.setInput('revisao', 1);
    await fixture.whenStable();

    expect(fixture.componentInstance.valorAtual()).toBe(3);
  });

  it('deve reescrever o campo com o número limitado', async () => {
    const { fixture, campo } = await criar(1);

    campo.value = '0';
    campo.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    expect(campo.value).toBe('1');
  });
});

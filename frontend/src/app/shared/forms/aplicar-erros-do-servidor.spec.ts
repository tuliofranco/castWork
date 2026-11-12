import { HttpErrorResponse } from '@angular/common/http';
import { FormArray, FormControl, FormGroup } from '@angular/forms';

import { aplicarErrosDoServidor } from './aplicar-erros-do-servidor';

describe('aplicarErrosDoServidor', () => {
  function erro400(errors: Record<string, string[]>): HttpErrorResponse {
    return new HttpErrorResponse({ status: 400, error: { title: 'Dados inválidos.', errors } });
  }

  it('deve pôr a mensagem do servidor no campo e devolver as que não têm campo', () => {
    const form = new FormGroup({ titulo: new FormControl(''), clienteId: new FormControl(1) });

    const semCampo = aplicarErrosDoServidor(form, erro400({ clienteId: ['Cliente não encontrado.'], geral: ['Outro erro.'] }));

    expect(form.controls.clienteId.getError('servidor')).toBe('Cliente não encontrado.');
    expect(form.controls.clienteId.touched).toBe(true);
    expect(form.controls.titulo.errors).toBeNull();
    expect(semCampo).toEqual(['Outro erro.']);
  });

  it('deve ignorar erros que não são 400', () => {
    const form = new FormGroup({ titulo: new FormControl('') });

    const semCampo = aplicarErrosDoServidor(form, new HttpErrorResponse({ status: 500 }));

    expect(semCampo).toEqual([]);
    expect(form.valid).toBe(true);
  });

  it('deve achar o campo de um item de lista ("itens[1].quantidade")', () => {
    const itens = new FormArray([new FormGroup({ quantidade: new FormControl(1) }), new FormGroup({ quantidade: new FormControl(0) })]);
    const form = new FormGroup({ itens });

    const semCampo = aplicarErrosDoServidor(form, erro400({ 'itens[1].quantidade': ['A quantidade deve ser maior que zero.'] }));

    expect(itens.at(1).controls.quantidade.getError('servidor')).toBe('A quantidade deve ser maior que zero.');
    expect(itens.at(0).controls.quantidade.errors).toBeNull();
    expect(semCampo).toEqual([]);
  });

  it('não deve marcar o campo quando a lista de mensagens vem vazia', () => {
    const form = new FormGroup({ titulo: new FormControl('') });

    aplicarErrosDoServidor(form, erro400({ titulo: [] }));

    expect(form.controls.titulo.errors).toBeNull();
  });
});

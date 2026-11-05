import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup } from '@angular/forms';

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
});

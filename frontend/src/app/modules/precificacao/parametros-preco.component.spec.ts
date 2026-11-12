import { TestBed } from '@angular/core/testing';
import type { ParametrosDePrecoDto } from '@data/precificacao/precos/dto/precos.dto';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import { ToastService } from '@shared/services/toast.service';

import { ParametrosPrecoComponent } from './parametros-preco.component';

describe('ParametrosPrecoComponent', () => {
  const parametros: ParametrosDePrecoDto = { perdaPercentual: 8, valorHora: 90, markup: 1.8, taxasPercentual: 0, arredondarPara: 10 };

  async function criar() {
    TestBed.configureTestingModule({
      providers: [
        { provide: PrecosService, useValue: {} },
        { provide: ToastService, useValue: { sucesso: vi.fn(), erro: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(ParametrosPrecoComponent);
    fixture.componentRef.setInput('parametros', parametros);
    fixture.componentRef.setInput('podeEditar', true);
    await fixture.whenStable();
    const markup = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('#markup')!;
    return { fixture, markup };
  }

  it('deve acompanhar os valores salvos quando a página relê', async () => {
    const { fixture, markup } = await criar();
    expect(markup.value).toBe('1.8');

    fixture.componentRef.setInput('parametros', { ...parametros, markup: 2 });
    await fixture.whenStable();

    expect(markup.value).toBe('2');
  });

  // Outro bloco da página (cotações, ligas) salva e a página relê: o que está sendo digitado aqui não pode sumir.
  it('não deve apagar a edição não salva quando a página relê', async () => {
    const { fixture, markup } = await criar();
    markup.value = '2.5';
    markup.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    fixture.componentRef.setInput('parametros', { ...parametros });
    await fixture.whenStable();

    expect(markup.value).toBe('2.5');
  });
});

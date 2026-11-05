import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

import { CampoComponent } from './campo.component';
import { EstadoListaComponent } from './estado-lista.component';
import { IniciaisComponent } from './iniciais.component';
import { PaginacaoComponent } from './paginacao.component';
import { QuantidadeComponent } from './quantidade.component';

async function renderizar<T>(componente: new () => T): Promise<{ el: HTMLElement; host: T; estabilizar: () => Promise<void> }> {
  const fixture = TestBed.createComponent(componente);
  await fixture.whenStable();
  return { el: fixture.nativeElement as HTMLElement, host: fixture.componentInstance, estabilizar: () => fixture.whenStable() };
}

describe('Componentes de UI', () => {
  describe('CampoComponent', () => {
    @Component({
      imports: [CampoComponent, ReactiveFormsModule],
      template: `<cw-campo rotulo="Nome" para="nome" [controle]="controle" [mensagens]="{ required: 'Informe o nome.' }" dica="Como aparece no PDF." [opcional]="true">
        <input id="nome" class="form-control" [formControl]="controle" />
      </cw-campo>`,
    })
    class Host {
      readonly controle = new FormControl('', Validators.required);
    }

    it('deve mostrar a dica até o campo ser tocado e então a mensagem do erro', async () => {
      const { el, host, estabilizar } = await renderizar(Host);
      expect(el.querySelector('label')!.textContent).toContain('opcional');
      expect(el.textContent).toContain('Como aparece no PDF.');

      host.controle.markAsTouched();
      await estabilizar();

      expect(el.querySelector('[role=alert]')!.textContent).toBe('Informe o nome.');
      expect(el.querySelector('cw-campo')!.classList).toContain('invalido');
    });

    it('deve mostrar o erro do servidor mesmo sem toque', async () => {
      const { el, host, estabilizar } = await renderizar(Host);

      host.controle.setErrors({ servidor: 'Já existe um cliente com este nome.' });
      await estabilizar();

      expect(el.querySelector('[role=alert]')!.textContent).toBe('Já existe um cliente com este nome.');
    });
  });

  describe('QuantidadeComponent', () => {
    @Component({
      imports: [QuantidadeComponent],
      template: `<cw-quantidade [valor]="valor()" rotulo="Elo" [espera]="0" [maximo]="3" (alterado)="registrar($event)" />`,
    })
    class Host {
      readonly valor = signal(2);
      readonly emitidos: number[] = [];

      // Como no uso real: o pai grava e devolve o valor novo.
      registrar(valor: number): void {
        this.emitidos.push(valor);
        this.valor.set(valor);
      }
    }

    it('deve somar, limitar ao máximo e ao mínimo, e emitir cada mudança', async () => {
      const { el, host, estabilizar } = await renderizar(Host);
      const mais = el.querySelector<HTMLButtonElement>('[aria-label="Aumentar Elo"]')!;
      const menos = el.querySelector<HTMLButtonElement>('[aria-label="Diminuir Elo"]')!;

      mais.click();
      await estabilizar();
      expect(mais.disabled).toBe(true);
      menos.click();
      menos.click();
      await estabilizar();

      expect(host.emitidos).toEqual([3, 2, 1]);
      expect(menos.disabled).toBe(true);
      expect(el.querySelector('input')!.value).toBe('1');
    });

    it('deve juntar toques seguidos numa única emissão quando há espera', async () => {
      vi.useFakeTimers();
      @Component({ imports: [QuantidadeComponent], template: `<cw-quantidade [valor]="1" rotulo="Elo" (alterado)="emitidos.push($event)" />` })
      class ComEspera {
        readonly emitidos: number[] = [];
      }
      const fixture = TestBed.createComponent(ComEspera);
      fixture.detectChanges();
      const mais = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('[aria-label="Aumentar Elo"]')!;

      mais.click();
      mais.click();
      mais.click();
      vi.advanceTimersByTime(500);

      expect(fixture.componentInstance.emitidos).toEqual([4]);
      vi.useRealTimers();
    });
  });

  describe('EstadoListaComponent', () => {
    @Component({
      imports: [EstadoListaComponent],
      template: `<cw-estado-lista [carregando]="carregando()" [erro]="erro()" [vazio]="vazio()" tituloVazio="Sem peças." (tentarDeNovo)="tentativas = tentativas + 1"><p id="conteudo">lista</p></cw-estado-lista>`,
    })
    class Host {
      readonly carregando = signal(true);
      readonly erro = signal(false);
      readonly vazio = signal(false);
      tentativas = 0;
    }

    it('deve passar por esqueleto, vazio, conteúdo e erro com tentar de novo', async () => {
      const { el, host, estabilizar } = await renderizar(Host);
      expect(el.querySelector('[aria-busy=true]')).not.toBeNull();

      host.carregando.set(false);
      host.vazio.set(true);
      await estabilizar();
      expect(el.textContent).toContain('Sem peças.');

      host.vazio.set(false);
      await estabilizar();
      expect(el.querySelector('#conteudo')).not.toBeNull();

      host.erro.set(true);
      await estabilizar();
      el.querySelector<HTMLButtonElement>('[role=alert] button')!.click();
      expect(host.tentativas).toBe(1);
      expect(el.querySelector('#conteudo')).toBeNull();
    });
  });

  describe('PaginacaoComponent e IniciaisComponent', () => {
    @Component({
      imports: [PaginacaoComponent, IniciaisComponent],
      template: `
        <cw-paginacao [total]="1" [pagina]="1" singular="peça" plural="peças" />
        <cw-paginacao [total]="45" [pagina]="2" singular="peça" plural="peças" [temAnterior]="true" [temProxima]="true" />
        <cw-iniciais nome="  maria da silva " /><cw-iniciais nome="Ana" />
      `,
    })
    class Host {}

    it('deve concordar o total no singular e esconder os botões quando há uma página só', async () => {
      const { el } = await renderizar(Host);
      const [unica, varias] = [...el.querySelectorAll('cw-paginacao')];

      expect(unica!.textContent?.trim()).toBe('1 peça');
      expect(unica!.querySelectorAll('button')).toHaveLength(0);
      expect(varias!.querySelector('.resumo')!.textContent).toBe('45 peças · página 2');
      expect([...el.querySelectorAll('cw-iniciais')].map((i) => i.textContent)).toEqual(['MS', 'A']);
    });
  });
});

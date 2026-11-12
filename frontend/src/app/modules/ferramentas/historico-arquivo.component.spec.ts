import { TestBed } from '@angular/core/testing';
import type { VersaoDoArquivoDto } from '@data/assistente/dto/ferramenta.dto';

import { HistoricoArquivoComponent } from './historico-arquivo.component';

describe('HistoricoArquivoComponent', () => {
  const versoes: readonly VersaoDoArquivoDto[] = [
    { id: 3, origem: 'Joalheiro', criadoEm: '2026-11-10T12:00:00Z', tamanho: 10, nome: null },
    { id: 2, origem: 'NomesDaPeca', criadoEm: '2026-11-10T11:00:00Z', tamanho: 10, nome: null },
  ];

  function abrir(podeMarcarFinal: boolean) {
    HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
      this.open = true;
    };
    TestBed.configureTestingModule({ imports: [HistoricoArquivoComponent] });
    const fixture = TestBed.createComponent(HistoricoArquivoComponent);
    fixture.componentRef.setInput('tipo', 'modelo');
    fixture.componentRef.setInput('versoes', versoes);
    fixture.componentRef.setInput('titulo', 'Elo Infinito 2,5 cm · Modelo 3D');
    fixture.componentRef.setInput('finalId', 2);
    fixture.componentRef.setInput('podeMarcarFinal', podeMarcarFinal);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('deve mostrar o título da peça, qual é o final e marcar outro', () => {
    const { fixture, el } = abrir(true);
    const marcadas: number[] = [];
    fixture.componentInstance.marcarFinal.subscribe((v) => marcadas.push(v.id));

    expect(el.querySelector('h2')?.textContent?.trim()).toBe('Elo Infinito 2,5 cm · Modelo 3D');
    const [atual, final] = [...el.querySelectorAll('li')];
    expect(final.querySelector('.final')?.textContent).toBe('final');
    expect([...final.querySelectorAll('button')].some((b) => b.textContent?.includes('Marcar como final'))).toBe(false);
    ([...atual.querySelectorAll('button')].find((b) => b.textContent?.includes('Marcar como final')) as HTMLButtonElement).click();
    expect(marcadas).toEqual([3]);
  });

  it('não deve oferecer marcar como final sem permissão', () => {
    const { el } = abrir(false);

    expect(el.textContent).not.toContain('Marcar como final');
  });
});

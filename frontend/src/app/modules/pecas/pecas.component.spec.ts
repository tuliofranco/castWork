import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { of, throwError } from 'rxjs';

import { PecasComponent } from './pecas.component';

// Mesmo locale do app (app.config): números com vírgula decimal.
registerLocaleData(localePt, 'pt-BR');

describe('PecasComponent', () => {
  const pagina = {
    totalCount: 3,
    pageInfo: { hasNextPage: false, endCursor: null },
    nodes: [
      { id: 1, nome: 'Elo oval', tipo: 'ELO', material: 'Ouro amarelo', teor: 750, pesoGramas: 0.35, ativo: true },
      { id: 2, nome: 'Turmalina', tipo: 'PEDRA', material: null, teor: null, pesoGramas: null, ativo: false },
      { id: 3, nome: 'Fecho lagosta', tipo: 'FECHO', material: null, teor: null, pesoGramas: null, ativo: true },
    ],
  };

  async function abrir() {
    const getPecas = vi.fn().mockReturnValue(of(pagina));
    TestBed.configureTestingModule({
      imports: [PecasComponent],
      providers: [provideRouter([]), { provide: LOCALE_ID, useValue: 'pt-BR' }, { provide: PecasService, useValue: { getPecas } }],
    });
    const fixture = TestBed.createComponent(PecasComponent);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const linhas = [...el.querySelectorAll('.cw-linha')].map((linha) =>
      [...linha.querySelectorAll('.nome, .meta, cw-selo, .peso')].map((parte) => parte.textContent?.replace(/\s+/g, ' ').trim()),
    );
    return { el, linhas, getPecas };
  }

  it('deve listar a biblioteca com tipo, material, teor e peso, sem valor', async () => {
    const { el, linhas, getPecas } = await abrir();

    expect(linhas).toEqual([
      ['Elo oval', 'Elo · Ouro amarelo · 750', '0,350 g'],
      ['Turmalina', 'Pedra', 'Inativa'],
      ['Fecho lagosta', 'Fecho'],
    ]);
    expect(getPecas).toHaveBeenCalledWith(expect.objectContaining({ termo: null, primeiros: 20 }));
    expect(getPecas.mock.calls[0]![0]).not.toHaveProperty('comCusto');
    expect(el.textContent).not.toContain('Sem custo');
    expect(el.querySelectorAll('.cw-linha')[1]!.classList).toContain('inativa');
  });

  it('deve ser só de busca: sem Nova peça e cada peça abre o detalhe de consulta, não a edição', async () => {
    const { el } = await abrir();

    expect(el.textContent).not.toContain('Nova peça');
    expect([...el.querySelectorAll('a.cw-linha')].map((a) => a.getAttribute('href'))).toEqual(['/1', '/2', '/3']);
    expect(el.querySelector('a[href$="editar"]')).toBeNull();
    expect(el.querySelector('cw-campo-busca')).not.toBeNull();
  });

  it('deve mostrar o erro com Tentar de novo quando a biblioteca não carrega', async () => {
    const getPecas = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('API fora')))
      .mockReturnValue(of({ ...pagina, totalCount: 1, nodes: [pagina.nodes[0]] }));
    TestBed.configureTestingModule({
      imports: [PecasComponent],
      providers: [provideRouter([]), { provide: LOCALE_ID, useValue: 'pt-BR' }, { provide: PecasService, useValue: { getPecas } }],
    });
    const fixture = TestBed.createComponent(PecasComponent);
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    const tentar = [...el.querySelectorAll<HTMLButtonElement>('[role=alert] button')].find((b) => b.textContent?.trim() === 'Tentar de novo');
    expect(el.querySelector('[role=alert]')!.textContent).toContain('Não foi possível carregar');
    expect(el.querySelector('.cw-linha')).toBeNull();

    tentar!.click();
    await fixture.whenStable();

    expect(getPecas).toHaveBeenCalledTimes(2);
    expect(el.querySelector('[role=alert]')).toBeNull();
    expect(el.querySelectorAll('.cw-linha').length).toBe(1);
  });
});

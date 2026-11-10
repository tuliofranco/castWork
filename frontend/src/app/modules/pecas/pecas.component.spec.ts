import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { of } from 'rxjs';

import { PecasComponent } from './pecas.component';

// Mesmo locale do app (app.config): números com vírgula decimal.
registerLocaleData(localePt, 'pt-BR');

describe('PecasComponent', () => {
  const pagina = {
    totalCount: 3,
    pageInfo: { hasNextPage: false, endCursor: null },
    nodes: [
      { id: 1, nome: 'Elo oval', tipo: 'ELO', material: 'Ouro amarelo', teor: 750, pesoGramas: 0.35, ativo: true, custo: 12.5 },
      { id: 2, nome: 'Turmalina', tipo: 'PEDRA', material: null, teor: null, pesoGramas: null, ativo: false, custo: null },
      { id: 3, nome: 'Fecho lagosta', tipo: 'FECHO', material: null, teor: null, pesoGramas: null, ativo: true, custo: null },
    ],
  };

  async function abrir(permissoes: readonly string[]) {
    const getPecas = vi.fn().mockReturnValue(of(pagina));
    TestBed.configureTestingModule({
      imports: [PecasComponent],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: PecasService, useValue: { getPecas } },
        { provide: PermissaoService, useValue: { temAlguma: (pedidas: readonly string[]) => pedidas.some((p) => permissoes.includes(p)) } },
      ],
    });
    const fixture = TestBed.createComponent(PecasComponent);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const linhas = [...el.querySelectorAll('.cw-linha')].map((linha) =>
      [...linha.querySelectorAll('.nome, .meta, cw-selo, .peso, .custo')].map((parte) => parte.textContent?.replace(/\s+/g, ' ').trim()),
    );
    return { el, linhas, getPecas };
  }

  it('deve listar a biblioteca com tipo, material, teor, peso e custo, marcando as peças sem custo', async () => {
    const { el, linhas, getPecas } = await abrir(['pecas.criar', 'pecas.editar', 'precificacao.ler']);

    expect(linhas).toEqual([
      ['Elo oval', 'Elo · Ouro amarelo · 750 (18k)', '0,350 g', 'R$ 12,50'],
      ['Turmalina', 'Pedra', 'Inativa'],
      ['Fecho lagosta', 'Fecho', 'Sem custo'],
    ]);
    expect(getPecas).toHaveBeenCalledWith(expect.objectContaining({ comCusto: true }));
    expect(el.querySelectorAll('.cw-linha')[1]!.classList).toContain('inativa');
    expect(el.textContent).toContain('Nova peça');
    expect([...el.querySelectorAll('a.cw-linha')].map((a) => a.getAttribute('href'))).toEqual(['/1/editar', '/3/editar']);
  });

  it('não deve pedir nem mostrar custo sem precificacao.ler, nem abrir edição sem pecas.editar', async () => {
    const { el, linhas, getPecas } = await abrir(['pecas.ler']);

    expect(linhas).toEqual([
      ['Elo oval', 'Elo · Ouro amarelo · 750 (18k)', '0,350 g'],
      ['Turmalina', 'Pedra', 'Inativa'],
      ['Fecho lagosta', 'Fecho'],
    ]);
    expect(getPecas).toHaveBeenCalledWith(expect.objectContaining({ comCusto: false }));
    expect(el.querySelectorAll('a.cw-linha')).toHaveLength(0);
    expect(el.textContent).not.toContain('Nova peça');
  });
});

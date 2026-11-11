import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import type { PecaDetalheDto } from '@data/pecas/pecas/dto/peca.dto';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { of } from 'rxjs';

import { PecaDetalheComponent } from './peca-detalhe.component';

registerLocaleData(localePt, 'pt-BR');

describe('PecaDetalheComponent', () => {
  const turmalina: PecaDetalheDto = {
    id: 7,
    nome: 'Turmalina 4 mm',
    tipo: 'PEDRA',
    material: 'Turmalina',
    teor: null,
    pesoGramas: 0.2,
    descricao: 'Lapidação oval',
    ativo: true,
    desenhos: [],
    historicoDeUso: [
      { projetoId: 2, projeto: 'Pulseira Mar', pecaDoProjetoId: 21, valor: 95, versoes: [] },
      { projetoId: 1, projeto: 'Colar Raízes', pecaDoProjetoId: 11, valor: 80, versoes: [{ versaoId: 5, numero: 1, nome: 'Base', status: 'PUBLICADA', quantidade: 3 }] },
    ],
  };

  async function abrir(permissoes: readonly string[], peca: PecaDetalheDto = turmalina) {
    const getPeca = vi.fn(() => of(peca));
    TestBed.configureTestingModule({
      imports: [PecaDetalheComponent],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: PecasService, useValue: { getPeca } },
        { provide: PermissaoService, useValue: { temAlguma: (pedidas: readonly string[]) => pedidas.some((p) => permissoes.includes(p)) } },
      ],
    });
    const fixture = TestBed.createComponent(PecaDetalheComponent);
    fixture.componentRef.setInput('id', '7');
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const usos = [...el.querySelectorAll('.uso')].map((uso) =>
      [...uso.querySelectorAll('.nome, .versoes li, .meta, .valor')].map((parte) => parte.textContent?.replace(/\s+/g, ' ').trim()),
    );
    return { el, usos, getPeca };
  }

  it('deve mostrar os dados e o histórico de uso com o valor em cada projeto', async () => {
    const { el, usos, getPeca } = await abrir(['pecas.ler', 'projetos.ler', 'precificacao.ler']);

    expect(el.querySelector('h1')?.textContent).toContain('Turmalina 4 mm');
    expect(el.querySelector('.dados')?.textContent).toContain('Lapidação oval');
    expect([...el.querySelectorAll('.dados dt')].map((dt) => dt.textContent?.trim())).toEqual(['Tipo', 'Material', 'Peso', 'Descrição']);
    expect(usos).toEqual([
      ['Pulseira Mar', 'Importada, ainda sem uso nas versões', 'R$ 95,00'],
      ['Colar Raízes', '1. Base · 3×', 'R$ 80,00'],
    ]);
    expect(getPeca).toHaveBeenCalledWith(7, { comHistorico: true, comValor: true });
  });

  it('deve mostrar a liga e o teor copiados numa peça de metal', async () => {
    const { el } = await abrir(['pecas.ler'], { ...turmalina, tipo: 'ELO', material: 'Ouro 18k amarelo', teor: 750 });

    const dados = [...el.querySelectorAll('.dados > div')].map((d) => [d.querySelector('dt')?.textContent?.trim(), d.querySelector('dd')?.textContent?.trim()]);
    expect(dados).toContainEqual(['Liga', 'Ouro 18k amarelo']);
    expect(dados).toContainEqual(['Teor', '750']);
  });

  it('deve ser só consulta, sem formulário nem botão de editar', async () => {
    const { el } = await abrir(['pecas.ler', 'pecas.editar', 'projetos.ler']);

    expect(el.querySelector('form')).toBeNull();
    expect(el.querySelectorAll('button')).toHaveLength(0);
  });

  it('não deve pedir o valor sem precificacao.ler nem o histórico sem projetos.ler', async () => {
    const semValor = await abrir(['pecas.ler', 'projetos.ler']);
    expect(semValor.usos.map((u) => u.at(-1))).toEqual(['Importada, ainda sem uso nas versões', '1. Base · 3×']);
    expect(semValor.getPeca).toHaveBeenCalledWith(7, { comHistorico: true, comValor: false });

    TestBed.resetTestingModule();
    const semHistorico = await abrir(['pecas.ler']);
    expect(semHistorico.el.textContent).not.toContain('Histórico de uso');
    expect(semHistorico.getPeca).toHaveBeenCalledWith(7, { comHistorico: false, comValor: false });
  });

  it('deve mostrar o desenho 2D e o modelo 3D da peça só para ver', async () => {
    const { el } = await abrir(['pecas.ler'], {
      ...turmalina,
      desenhos: [
        { ferramenta: 'desenho-2d', copiadoEm: '2026-11-10T15:00:00Z', tamanho: 120 },
        { ferramenta: 'modelo-3d', copiadoEm: '2026-11-10T15:00:00Z', tamanho: 4000 },
      ],
    });

    const desenhos = [...el.querySelectorAll<HTMLAnchorElement>('.desenho a.ferramenta')];
    expect(desenhos.map((a) => a.querySelector('strong')?.textContent)).toEqual(['Desenho 2D', 'Modelo 3D']);
    expect(desenhos.map((a) => a.getAttribute('href'))).toEqual(['/pecas/7/desenhos/desenho-2d', '/pecas/7/desenhos/modelo-3d']);
    expect(desenhos[0].textContent).toContain('Só para ver · publicado em 10/11/2026');
  });

  it('deve dizer quando a peça foi publicada sem desenho', async () => {
    const { el } = await abrir(['pecas.ler']);

    expect(el.querySelector('.desenho')?.textContent).toContain('A peça foi publicada sem desenho 2D nem modelo 3D.');
  });
});

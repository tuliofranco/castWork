import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { ImagemGuardadaDto } from '@data/assistente/dto/ferramenta.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { DownloadService } from '@shared/services/download.service';
import { of } from 'rxjs';

import { DesenhoDaPecaComponent, emFundoEscuro } from './desenho-da-peca.component';

describe('emFundoEscuro', () => {
  it('deve trocar fundo branco por preto e linhas pretas por brancas, mantendo as cores das cotas', () => {
    const svg = '<rect fill="#FFFFFF"/><path stroke="#000000"/><path stroke="rgb(0, 0, 0)"/><text fill="#ffff00"/><line stroke="green"/>';

    expect(emFundoEscuro(svg)).toBe('<rect fill="#000000"/><path stroke="#ffffff"/><path stroke="#ffffff"/><text fill="#ffff00"/><line stroke="green"/>');
  });
});

describe('DesenhoDaPecaComponent', () => {
  const png = new Blob([new Uint8Array([0x89, 0x50, 0x4e, 0x47])], { type: 'image/png' });

  async function abrir(imagens: readonly ImagemGuardadaDto[]) {
    const pecas = {
      getImagensDoDesenho: vi.fn(() => of(imagens)),
      getImagemDoDesenho: vi.fn(() => of(png)),
      getPreviaDoDesenho: vi.fn(() => of(png)),
      getDesenho: vi.fn(() => of(new Blob(['3D']))),
      getPeca: vi.fn(() => of({ nome: 'Elo Infinito 2,5 cm' })),
    };
    const download = { salvar: vi.fn() };
    TestBed.configureTestingModule({
      imports: [DesenhoDaPecaComponent],
      providers: [provideRouter([]), { provide: PecasService, useValue: pecas }, { provide: FerramentasService, useValue: {} }, { provide: ProjetosService, useValue: {} }, { provide: DownloadService, useValue: download }],
    });
    const fixture = TestBed.createComponent(DesenhoDaPecaComponent);
    fixture.componentRef.setInput('id', '7');
    fixture.componentRef.setInput('ferramenta', 'modelo-3d');
    fixture.detectChanges();
    await fixture.whenStable();
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
    return { fixture, pecas, download, el: fixture.nativeElement as HTMLElement };
  }

  it('deve mostrar as imagens guardadas com um botão por vista', async () => {
    const vistas = ['iso', 'frente', 'topo', 'lado'] as const;
    const { fixture, pecas, el } = await abrir(vistas.map((vista) => ({ vista, tipo: 'image/png' })));

    const botoes = [...el.querySelectorAll('.vistas button')].map((b) => b.textContent?.trim());
    expect(botoes).toEqual(['Iso', 'Frente', 'Topo', 'Lado']);
    expect(pecas.getImagemDoDesenho).toHaveBeenCalledWith(7, 'modelo-3d', 'iso');
    expect(pecas.getPreviaDoDesenho).not.toHaveBeenCalled();
    expect(el.querySelector('img')).not.toBeNull();

    (el.querySelectorAll('.vistas button')[2] as HTMLButtonElement).click();
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
    expect(pecas.getImagemDoDesenho).toHaveBeenCalledWith(7, 'modelo-3d', 'topo');
    expect(el.querySelector('.vistas .ativa')?.textContent?.trim()).toBe('Topo');
  });

  it('deve usar a prévia sob demanda quando não há imagem guardada', async () => {
    const { pecas, el } = await abrir([]);

    expect(pecas.getPreviaDoDesenho).toHaveBeenCalledWith(7, 'modelo-3d');
    expect(el.querySelector('.vistas')).toBeNull();
    expect(el.querySelector('img')).not.toBeNull();
  });

  it('deve aproximar pelos botões e voltar a 100% ao ajustar', async () => {
    const { fixture, el } = await abrir([{ vista: 'iso', tipo: 'image/png' }]);
    const [menos, escala, mais] = [...el.querySelectorAll('.zoom button')] as HTMLButtonElement[];

    mais.click();
    mais.click();
    fixture.detectChanges();
    expect(escala.textContent?.trim()).toBe('156%');
    expect((el.querySelector('img') as HTMLImageElement).style.transform).toContain('scale(1.5625)');

    escala.click();
    menos.click();
    fixture.detectChanges();
    expect(escala.textContent?.trim()).toBe('80%');
  });

  it('deve dar o nome da peça ao título e aos arquivos baixados', async () => {
    const { fixture, download, el } = await abrir([{ vista: 'iso', tipo: 'image/png' }]);

    expect(el.querySelector('h1')?.textContent?.trim()).toBe('Elo Infinito 2,5 cm · Modelo 3D');
    (el.querySelector('.btn-secundario') as HTMLButtonElement).click();
    await new Promise((r) => setTimeout(r));
    expect(download.salvar).toHaveBeenCalledWith(expect.any(Blob), 'Elo Infinito 2,5 cm.json');

    const baixarImagem = [...el.querySelectorAll('button')].find((b) => b.textContent?.includes('Baixar imagem')) as HTMLButtonElement;
    baixarImagem.click();
    fixture.detectChanges();
    expect(download.salvar).toHaveBeenCalledWith(png, 'Elo Infinito 2,5 cm - Iso.png');
  });
});

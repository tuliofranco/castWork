import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { ListaDeArquivosDto } from '@data/assistente/dto/ferramenta.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { BriefingService } from '@data/ferramentas/briefing/briefing.service';
import { DownloadService } from '@shared/services/download.service';
import { Subject, of, throwError } from 'rxjs';

import { ArquivosVersaoComponent } from './arquivos-versao.component';

describe('ArquivosVersaoComponent', () => {
  const lista = (totalRegistros: number): ListaDeArquivosDto => ({ lista: [], pagina: 1, totalPaginas: 1, totalRegistros }) as unknown as ListaDeArquivosDto;
  const getArquivosDaVersao = vi.fn();

  beforeAll(() => {
    // O jsdom não abre <dialog> modal.
    HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
      this.open = true;
    };
  });

  async function abrir() {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: FerramentasService, useValue: { getArquivosDaVersao } },
        { provide: BriefingService, useValue: { getBriefing: () => of(null) } },
        { provide: DownloadService, useValue: {} },
      ],
    });
    const fixture = TestBed.createComponent(ArquivosVersaoComponent);
    fixture.componentRef.setInput('projetoId', 1);
    fixture.componentRef.setInput('versaoId', 20);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    el.querySelector<HTMLButtonElement>('button.pasta')!.click();
    await fixture.whenStable();
    return { fixture, el };
  }

  const botao = (el: HTMLElement, texto: string) => [...el.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim() === texto);

  it('deve avisar quando a listagem falha e carregar ao tentar de novo', async () => {
    getArquivosDaVersao.mockReset().mockReturnValueOnce(throwError(() => new Error('API fora'))).mockReturnValue(of(lista(0)));
    const { fixture, el } = await abrir();

    expect(el.querySelector('[role=alert]')!.textContent).toContain('Não foi possível carregar os arquivos.');

    botao(el, 'Tentar de novo')!.click();
    await fixture.whenStable();

    expect(el.querySelector('[role=alert]')).toBeNull();
    expect(el.textContent).toContain('Nenhum arquivo ainda.');
  });

  it('deve descartar a resposta da aba que já foi deixada', async () => {
    const arquivadosLentos = new Subject<ListaDeArquivosDto>();
    getArquivosDaVersao.mockReset().mockImplementation((_p: number, _v: number, arquivados: boolean) => (arquivados ? arquivadosLentos : of(lista(0))));
    const { fixture, el } = await abrir();

    botao(el, 'Arquivados')!.click();
    await fixture.whenStable();
    // Enquanto a lista carrega, as abas somem: o caminho de volta é fechar e reabrir a pasta, que abre em "Arquivos".
    el.querySelector<HTMLButtonElement>('button.pasta')!.click();
    await fixture.whenStable();

    // A resposta lenta dos arquivados chega depois de a pasta ter voltado para "Arquivos".
    arquivadosLentos.next(lista(7));
    arquivadosLentos.complete();
    await fixture.whenStable();

    expect(el.textContent).toContain('Nenhum arquivo ainda.');
    expect(el.textContent).not.toContain('7 arquivo');
  });
});

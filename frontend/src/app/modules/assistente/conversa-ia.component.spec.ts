import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import type { ConversaFerramentaDto } from '@data/assistente/dto/ferramenta.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { ToastService } from '@shared/services/toast.service';
import { NEVER, of, throwError } from 'rxjs';

import { ConversaIaComponent } from './conversa-ia.component';

describe('ConversaIaComponent', () => {
  const conversa = (mudancas: Partial<ConversaFerramentaDto> = {}): ConversaFerramentaDto => ({
    mensagens: [],
    arquivoId: 5,
    processando: false,
    chat: 1,
    chatsAnteriores: 0,
    ...mudancas,
  });
  const ferramentas = {
    getConversa: vi.fn(),
    postConversa: vi.fn(),
    deleteConversa: vi.fn(() => of(undefined)),
    acompanhar: vi.fn(() => NEVER),
    caminhoDaImagem: vi.fn(() => ''),
  };
  interface Interno { enviar(fala: object): Promise<void>; apagar(): Promise<void> }

  async function abrir() {
    Object.values(ferramentas).forEach((f) => f.mockClear());
    ferramentas.getConversa.mockReset().mockReturnValueOnce(of(conversa()));
    TestBed.configureTestingModule({
      providers: [
        { provide: FerramentasService, useValue: ferramentas },
        { provide: ToastService, useValue: { erro: vi.fn(), sucesso: vi.fn() } },
      ],
    });
    // Só a lógica da conversa: o chat em si (cw-ferramenta-ia) tem os próprios specs.
    TestBed.overrideComponent(ConversaIaComponent, { set: { template: '', imports: [] } });
    const fixture = TestBed.createComponent(ConversaIaComponent);
    for (const [nome, valor] of Object.entries({ projetoId: 1, escopo: 20, ferramenta: 'desenho-2d', titulo: 'Desenho 2D', voltarPara: '/' })) {
      fixture.componentRef.setInput(nome, valor);
    }
    await fixture.whenStable();
    const componente = fixture.componentInstance;
    const eventos: string[] = [];
    componente.arquivoMudou.subscribe(() => eventos.push('arquivoMudou'));
    componente.respondeu.subscribe(() => eventos.push('respondeu'));
    return { componente, interno: componente as unknown as Interno, eventos };
  }

  const fala = { texto: 'Aumente o aro', imagens: [], arquivos: [], modelo: null, esforco: null };

  afterEach(() => vi.useRealTimers());

  it('deve continuar acompanhando a IA quando a conexão cai no meio do envio', async () => {
    const { componente, interno, eventos } = await abrir();
    vi.useFakeTimers();
    ferramentas.postConversa.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    ferramentas.getConversa
      .mockReturnValueOnce(of(conversa({ processando: true })))
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 0 })))
      .mockReturnValueOnce(of(conversa({ arquivoId: 9 })));

    const envio = interno.enviar(fala);
    await vi.advanceTimersByTimeAsync(4000);

    // A IA segue trabalhando no servidor: a tela não dá o turno por encerrado.
    expect(componente.pensando()).toBe(true);
    expect(eventos).toEqual([]);

    // Uma consulta que falha não encerra o acompanhamento.
    await vi.advanceTimersByTimeAsync(4000);
    expect(componente.pensando()).toBe(true);

    await vi.advanceTimersByTimeAsync(4000);
    await envio;

    expect(componente.pensando()).toBe(false);
    expect(eventos).toEqual(['respondeu', 'arquivoMudou']);
  });

  it('não deve acompanhar quando a API recusa a mensagem', async () => {
    const { componente, interno, eventos } = await abrir();
    ferramentas.postConversa.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 422 })));

    await interno.enviar(fala);

    expect(componente.pensando()).toBe(false);
    expect(ferramentas.getConversa).toHaveBeenCalledTimes(1);
    expect(eventos).toEqual([]);
  });

  it('não deve apagar o chat com a IA trabalhando', async () => {
    const { componente, interno } = await abrir();
    componente.pensando.set(true);

    await interno.apagar();

    expect(ferramentas.deleteConversa).not.toHaveBeenCalled();
  });
});

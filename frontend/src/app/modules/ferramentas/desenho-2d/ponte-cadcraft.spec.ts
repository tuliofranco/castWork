import { PonteCadCraft } from './ponte-cadcraft';

describe('PonteCadCraft', () => {
  /** Um iframe com o CADCraft simulado: só o que a ponte usa para ler o título e apertar ⌘S. */
  function criar(titulo = 'desenho.dxf') {
    const canvas = { focus: vi.fn() };
    const documento = { title: titulo, getElementById: () => canvas, dispatchEvent: vi.fn() };
    const janela = {
      document: documento,
      navigator: { userAgent: 'Macintosh' },
      KeyboardEvent: class {
        constructor(
          readonly type: string,
          readonly init: Record<string, unknown>,
        ) {}
      },
      location: { replace: vi.fn(), pathname: '/cadcraft/' },
    };
    const iframe = { contentWindow: janela, contentDocument: documento, src: '' } as unknown as HTMLIFrameElement;
    const peloJoalheiro = vi.fn();
    const ponte = new PonteCadCraft(iframe, peloJoalheiro);
    return { ponte, documento, janela, peloJoalheiro, interno: ponte as unknown as { esperandoSalvar: ((arquivo: Blob) => void) | null } };
  }

  afterEach(() => vi.useRealTimers());

  it('deve ler as alterações pelo "•" que o CADCraft põe no título', () => {
    expect(criar('desenho.dxf').ponte.temAlteracoes()).toBe(false);
    expect(criar('• desenho.dxf').ponte.temAlteracoes()).toBe(true);
  });

  it('deve apertar ⌘S e devolver o arquivo que o CADCraft entregar', async () => {
    const { ponte, documento, interno } = criar();
    const dxf = new Blob(['0\nEOF\n']);

    const salvo = ponte.salvar();
    await vi.waitFor(() => expect(interno.esperandoSalvar).not.toBeNull());
    interno.esperandoSalvar!(dxf);

    expect(await salvo).toBe(dxf);
    const teclas = documento.dispatchEvent.mock.calls.map(([e]) => (e as { type: string; init: { key: string; metaKey: boolean } }));
    expect(teclas.map((t) => t.type)).toEqual(['keydown', 'keyup']);
    expect(teclas[0]!.init).toMatchObject({ key: 's', metaKey: true });
  });

  it('não deve deixar a espera presa depois do tempo limite', async () => {
    vi.useFakeTimers();
    const { ponte, interno } = criar();

    const salvo = ponte.salvar();
    const falha = expect(salvo).rejects.toThrow('O CADCraft não salvou o desenho.');
    await vi.advanceTimersByTimeAsync(10_000);
    await falha;

    // Sem isto, o próximo ⌘S do joalheiro seria entregue a esta chamada que já desistiu.
    expect(interno.esperandoSalvar).toBeNull();
  });

  it('deve descarregar sem criar entrada no histórico do navegador', () => {
    const { ponte, janela } = criar();

    ponte.descarregar();

    expect(janela.location.replace).toHaveBeenCalledWith('about:blank');
  });
});

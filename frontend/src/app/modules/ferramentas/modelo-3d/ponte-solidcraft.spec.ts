import { PonteSolidCraft } from './ponte-solidcraft';

describe('PonteSolidCraft', () => {
  /** Um iframe com o SolidCraft simulado: `request` responde conforme o método. */
  function criar(request?: (metodo: string, parametros?: Record<string, unknown>) => Promise<unknown>) {
    const replace = vi.fn();
    const janela = { solidcraft: request ? { request: vi.fn(request) } : undefined, location: { replace, pathname: '/solidcraft/' } };
    const iframe = { contentWindow: janela, src: '' } as unknown as HTMLIFrameElement;
    return { ponte: new PonteSolidCraft(iframe, () => undefined), janela, replace };
  }

  afterEach(() => vi.useRealTimers());

  it('deve dizer que não há alterações enquanto o SolidCraft não subiu', async () => {
    expect(await criar().ponte.temAlteracoes()).toBe(false);
  });

  it('deve ler do SolidCraft se o projeto tem alterações', async () => {
    const { ponte } = criar(() => Promise.resolve({ name: 'm', revision: 3, dirty: true, text: '{}' }));

    expect(await ponte.temAlteracoes()).toBe(true);
  });

  it('deve exportar, guardar e só então avisar o SolidCraft de que a revisão está salva', async () => {
    const ordem: string[] = [];
    const { ponte, janela } = criar((metodo) => {
      ordem.push(metodo);
      return Promise.resolve(metodo === 'document.export' ? { name: 'm', revision: 7, dirty: true, text: '{"a":1}' } : undefined);
    });

    await ponte.salvar(async (arquivo) => {
      ordem.push(`guardar:${await arquivo.text()}`);
    });

    expect(ordem).toEqual(['document.export', 'guardar:{"a":1}', 'document.saved']);
    expect(janela.solidcraft!.request).toHaveBeenLastCalledWith('document.saved', { revision: 7 });
  });

  it('não deve marcar como salvo quando guardar falha', async () => {
    const { ponte, janela } = criar(() => Promise.resolve({ name: 'm', revision: 7, dirty: true, text: '{}' }));

    await expect(ponte.salvar(() => Promise.reject(new Error('API fora')))).rejects.toThrow('API fora');

    expect(janela.solidcraft!.request).toHaveBeenCalledTimes(1);
  });

  it('deve desistir com erro quando o SolidCraft não responde', async () => {
    vi.useFakeTimers();
    const { ponte } = criar(() => new Promise(() => undefined));

    const pedido = ponte.temAlteracoes();
    const falha = expect(pedido).rejects.toThrow('O SolidCraft não respondeu a tempo.');
    await vi.advanceTimersByTimeAsync(60_000);

    await falha;
  });

  it('deve descarregar sem criar entrada no histórico do navegador', () => {
    const { ponte, replace } = criar();

    ponte.descarregar();

    expect(replace).toHaveBeenCalledWith('about:blank');
  });
});

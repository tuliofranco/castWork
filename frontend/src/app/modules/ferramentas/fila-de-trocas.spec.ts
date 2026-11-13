import { FilaDeTrocas } from './fila-de-trocas';

describe('FilaDeTrocas', () => {
  /** Uma troca que só termina quando o teste libera. */
  function trocaControlada() {
    const liberar: (() => void)[] = [];
    const trocar = vi.fn(() => new Promise<void>((pronto) => liberar.push(pronto)));
    return { trocar, liberar };
  }
  const pausa = () => new Promise((resolve) => setTimeout(resolve));

  it('deve fazer uma troca por vez e mais uma para os pedidos que chegaram durante ela', async () => {
    const { trocar, liberar } = trocaControlada();
    const fila = new FilaDeTrocas(Promise.resolve(), trocar);

    const primeira = fila.pedir();
    await pausa();
    const segunda = fila.pedir();
    const terceira = fila.pedir();

    expect(segunda).toBe(primeira);
    expect(terceira).toBe(primeira);
    expect(trocar).toHaveBeenCalledTimes(1);
    expect(fila.trocando()).toBe(true);

    liberar[0]!();
    await pausa();
    expect(trocar).toHaveBeenCalledTimes(2);
    expect(fila.trocando()).toBe(true);

    liberar[1]!();
    await primeira;
    expect(trocar).toHaveBeenCalledTimes(2);
    expect(fila.trocando()).toBe(false);
    expect(fila.atual).toBeNull();
  });

  it('deve esperar a abertura antes da primeira troca', async () => {
    const { trocar, liberar } = trocaControlada();
    let abrir!: () => void;
    const fila = new FilaDeTrocas(new Promise<void>((pronto) => (abrir = pronto)), trocar);

    const pedido = fila.pedir();
    await pausa();
    expect(trocar).not.toHaveBeenCalled();
    expect(fila.trocando()).toBe(true);

    abrir();
    await pausa();
    expect(trocar).toHaveBeenCalledTimes(1);
    liberar[0]!();
    await pedido;
  });

  it('deve liberar a fila mesmo se a troca rejeitar', async () => {
    const fila = new FilaDeTrocas(Promise.resolve(), () => Promise.reject(new Error('falhou')));

    await expect(fila.pedir()).rejects.toThrow('falhou');

    expect(fila.trocando()).toBe(false);
    expect(fila.atual).toBeNull();
  });
});

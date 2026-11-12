import { TestBed } from '@angular/core/testing';

import { DownloadService } from './download.service';

describe('DownloadService', () => {
  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('deve baixar pelo link, tirá-lo do documento e liberar a URL só depois de 60 s', () => {
    vi.useFakeTimers();
    const criar = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:arquivo');
    const revogar = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    let noDocumento = false;
    const clique = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      noDocumento = document.body.contains(this);
    });
    const arquivo = new Blob(['dxf']);

    TestBed.inject(DownloadService).salvar(arquivo, 'Elo oval.dxf');
    const clicado = clique.mock.contexts[0] as HTMLAnchorElement;

    expect(criar).toHaveBeenCalledWith(arquivo);
    expect(clicado.download).toBe('Elo oval.dxf');
    expect(clicado.getAttribute('href')).toBe('blob:arquivo');
    expect(noDocumento).toBe(true);
    expect(document.body.contains(clicado)).toBe(false);

    vi.advanceTimersByTime(59_999);
    expect(revogar).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1);
    expect(revogar).toHaveBeenCalledWith('blob:arquivo');
  });
});

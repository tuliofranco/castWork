import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ImagemProtegidaComponent } from './imagem-protegida.component';

describe('ImagemProtegidaComponent', () => {
  async function criar(src = 'imagens/a') {
    let n = 0;
    const criarUrl = vi.spyOn(URL, 'createObjectURL').mockImplementation(() => `blob:${++n}`);
    const revogar = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(ImagemProtegidaComponent);
    fixture.componentRef.setInput('src', src);
    fixture.componentRef.setInput('rotulo', 'Vista frontal');
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    return { fixture, el, http: TestBed.inject(HttpTestingController), criarUrl, revogar, miniatura: el.querySelector<HTMLButtonElement>('button.miniatura')! };
  }

  afterEach(() => vi.restoreAllMocks());

  it('deve mostrar a imagem que veio da API', async () => {
    const { fixture, el, http } = await criar();

    http.expectOne('imagens/a').flush(new Blob(['png']));
    await fixture.whenStable();

    expect(el.querySelector('.miniatura img')!.getAttribute('src')).toBe('blob:1');
  });

  it('deve oferecer tentar de novo quando a imagem não vem', async () => {
    const { fixture, el, http, miniatura } = await criar();

    http.expectOne('imagens/a').flush(new Blob(['erro']), { status: 500, statusText: 'Erro' });
    await fixture.whenStable();

    expect(el.querySelector('.miniatura img')).toBeNull();
    expect(miniatura.getAttribute('aria-label')).toBe('Imagem indisponível: Vista frontal. Tentar de novo');
    expect(miniatura.disabled).toBe(false);

    miniatura.click();
    await fixture.whenStable();
    http.expectOne('imagens/a').flush(new Blob(['png']));
    await fixture.whenStable();

    expect(el.querySelector('.miniatura img')).not.toBeNull();
    expect(miniatura.getAttribute('aria-label')).toBe('Ampliar Vista frontal');
  });

  it('deve liberar a imagem anterior quando o endereço muda', async () => {
    const { fixture, el, http, revogar } = await criar();
    http.expectOne('imagens/a').flush(new Blob(['png']));
    await fixture.whenStable();

    fixture.componentRef.setInput('src', 'imagens/b');
    await fixture.whenStable();

    expect(revogar).toHaveBeenCalledWith('blob:1');
    expect(el.querySelector('.miniatura img')).toBeNull();

    http.expectOne('imagens/b').flush(new Blob(['png']));
    await fixture.whenStable();
    expect(el.querySelector('.miniatura img')!.getAttribute('src')).toBe('blob:2');
  });

  it('deve usar direto um endereço local, sem pedir à API', async () => {
    const { el, http } = await criar('blob:local');

    http.expectNone('blob:local');
    expect(el.querySelector('.miniatura img')!.getAttribute('src')).toBe('blob:local');
  });
});

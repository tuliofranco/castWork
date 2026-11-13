import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { sairDaFerramentaGuard } from './sair-da-ferramenta.guard';

describe('sairDaFerramentaGuard', () => {
  const rota = {} as ActivatedRouteSnapshot;
  const para = (url: string) => ({ url }) as RouterStateSnapshot;

  it('deve perguntar à tela antes de sair', async () => {
    const tela = { podeSair: vi.fn(() => Promise.resolve(false)) };

    expect(await sairDaFerramentaGuard(tela, rota, para('/x'), para('/projetos/1'))).toBe(false);
    expect(tela.podeSair).toHaveBeenCalledTimes(1);
  });

  it('deve sair direto para o login, sem perguntar', () => {
    const tela = { podeSair: vi.fn(() => Promise.resolve(false)) };

    expect(sairDaFerramentaGuard(tela, rota, para('/x'), para('/login?voltar=x'))).toBe(true);
    expect(tela.podeSair).not.toHaveBeenCalled();
  });
});

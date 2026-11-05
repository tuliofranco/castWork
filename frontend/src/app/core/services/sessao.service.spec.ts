import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { criarTokenMock } from '@data/auth/login/mocks/token-payload.mock';

import { AuthService } from './auth.service';
import { PermissaoService } from './permissao.service';
import { SessaoService } from './sessao.service';

describe('SessaoService e PermissaoService', () => {
  const token = signal<string | null>(null);

  beforeEach(() => {
    token.set(null);
    TestBed.configureTestingModule({ providers: [{ provide: AuthService, useValue: { token } }] });
  });

  it('deve expor usuário e ateliê a partir do token', () => {
    token.set(criarTokenMock({ sub: 'u1', nome: 'Ana', email: 'ana@x.com', atelieNome: 'Ateliê Sol' }));
    const sessao = TestBed.inject(SessaoService);
    expect(sessao.usuario()).toEqual({ id: 'u1', nome: 'Ana', email: 'ana@x.com' });
    expect(sessao.atelie()).toEqual({ nome: 'Ateliê Sol' });
  });

  it('deve devolver null e nenhuma permissão sem token', () => {
    const sessao = TestBed.inject(SessaoService);
    expect(sessao.usuario()).toBeNull();
    expect(sessao.permissoes().size).toBe(0);
  });

  it('deve aceitar permissões vindas como string única', () => {
    token.set(criarTokenMock({ permissoes: 'pecas.ler' }));
    expect([...TestBed.inject(SessaoService).permissoes()]).toEqual(['pecas.ler']);
  });

  it('deve recalcular quando o token muda (troca de usuário)', () => {
    const permissao = TestBed.inject(PermissaoService);
    token.set(criarTokenMock({ permissoes: ['pecas.ler'] }));
    expect(permissao.temAlguma(['clientes.ler'])).toBe(false);

    token.set(criarTokenMock({ permissoes: ['clientes.ler'] }));
    expect(permissao.temAlguma(['clientes.ler'])).toBe(true);
  });

  it.each([
    [[], true],
    [['pecas.ler'], true],
    [['clientes.ler', 'pecas.ler'], true],
    [['clientes.ler'], false],
  ])('temAlguma(%j) deve ser %s', (pedidas, esperado) => {
    token.set(criarTokenMock({ permissoes: ['pecas.ler', 'pecas.editar'] }));
    expect(TestBed.inject(PermissaoService).temAlguma(pedidas)).toBe(esperado);
  });

  it('temTodas deve exigir todas as permissões', () => {
    token.set(criarTokenMock({ permissoes: ['pecas.ler', 'pecas.editar'] }));
    const permissao = TestBed.inject(PermissaoService);
    expect(permissao.temTodas(['pecas.ler', 'pecas.editar'])).toBe(true);
    expect(permissao.temTodas(['pecas.ler', 'clientes.ler'])).toBe(false);
  });
});

import { TestBed } from '@angular/core/testing';
import { criarTokenMock } from '@data/auth/login/mocks/token-payload.mock';

import { JwtService } from './jwt.service';

describe('JwtService', () => {
  let service: JwtService;
  const agora = () => Math.floor(Date.now() / 1000);

  beforeEach(() => {
    service = TestBed.inject(JwtService);
  });

  it('deve decodificar o payload, inclusive com acentos', () => {
    const token = criarTokenMock({ nome: 'Joana Ourives', atelieNome: 'Ateliê Pedra' });
    expect(service.decodificar<{ atelieNome: string }>(token)?.atelieNome).toBe('Ateliê Pedra');
  });

  it.each([null, '', 'nao-e-jwt', 'a.b.c'])('deve devolver null para token inválido (%s)', (token) => {
    expect(service.decodificar(token)).toBeNull();
  });

  it('deve considerar válido um token que vence no futuro', () => {
    expect(service.expirado(criarTokenMock({ exp: agora() + 3600 }))).toBe(false);
  });

  it('deve considerar vencido um token com exp no passado', () => {
    expect(service.expirado(criarTokenMock({ exp: agora() - 1 }))).toBe(true);
  });

  it('deve antecipar a expiração pela margem', () => {
    const token = criarTokenMock({ exp: agora() + 5 });
    expect(service.expirado(token)).toBe(false);
    expect(service.expirado(token, 10)).toBe(true);
  });

  it('deve considerar vencido token ausente ou malformado', () => {
    expect(service.expirado(null)).toBe(true);
    expect(service.expirado('lixo')).toBe(true);
  });
});

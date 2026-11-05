import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';

/**
 * Fachada sobre o JwtHelperService do @auth0/angular-jwt.
 * Isola a lib: se um dia trocarmos de biblioteca, só este arquivo muda.
 */
@Injectable({ providedIn: 'root' })
export class JwtService {
  private readonly helper = new JwtHelperService();

  /** Devolve null para token ausente ou malformado, em vez de lançar exceção. */
  decodificar<T>(token: string | null): T | null {
    if (!token) return null;
    try {
      return this.helper.decodeToken<T>(token);
    } catch {
      return null;
    }
  }

  /**
   * `margemSegundos` antecipa a expiração: um token que vence em 5 s já é tratado como vencido,
   * para não sair uma requisição que chegará ao servidor depois do vencimento.
   */
  expirado(token: string | null, margemSegundos = 0): boolean {
    if (!token || !this.decodificar(token)) return true;
    try {
      return this.helper.isTokenExpired(token, margemSegundos);
    } catch {
      return true;
    }
  }
}

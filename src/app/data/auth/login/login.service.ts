import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { type Observable, delay, of, throwError } from 'rxjs';

import type { PostLoginRespostaDto } from './dto/post-login-resposta.dto';
import type { PostLoginDto } from './dto/post-login.dto';
import { codificarTokenMock } from './mocks/codificar-token.mock';
import { SENHA_DEMO, USUARIOS_DEMO } from './mocks/usuarios-demo.mock';

const OITO_HORAS_EM_SEGUNDOS = 8 * 60 * 60;

/** Data service: só faz a requisição e devolve o DTO. Quem grava o token é o AuthService. */
@Injectable({ providedIn: 'root' })
export class LoginService {
  private readonly http = inject(HttpClient);
  private readonly ambiente = inject(AMBIENTE);

  postLogin(dto: PostLoginDto): Observable<PostLoginRespostaDto> {
    if (this.ambiente.usarMocks) return this.postLoginMock(dto);
    return this.http.post<PostLoginRespostaDto>('auth/login', dto);
  }

  private postLoginMock(dto: PostLoginDto): Observable<PostLoginRespostaDto> {
    const usuario = USUARIOS_DEMO[dto.email.toLowerCase()];
    if (!usuario || dto.senha !== SENHA_DEMO) {
      return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' }));
    }
    const exp = Math.floor(Date.now() / 1000) + OITO_HORAS_EM_SEGUNDOS;
    // delay simula latência para dar para ver o estado "entrando..." no botão.
    return of({ accessToken: codificarTokenMock({ ...usuario, exp }), refreshToken: 'refresh-mock' }).pipe(delay(400));
  }
}

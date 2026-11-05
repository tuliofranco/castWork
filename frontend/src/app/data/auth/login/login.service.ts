import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { semAuth, semErro } from '@shared/injection-tokens/http-context.tokens';
import { type Observable, delay, of, throwError } from 'rxjs';

import type { PostLoginRespostaDto } from './dto/post-login-resposta.dto';
import type { PostLoginDto } from './dto/post-login.dto';
import { emitirTokensMock } from './mocks/emitir-tokens.mock';
import { SENHA_DEMO } from './mocks/usuarios-demo.mock';

/** Data service: só faz a requisição e devolve o DTO. Quem grava o token é o AuthService. */
@Injectable({ providedIn: 'root' })
export class LoginService {
  private readonly http = inject(HttpClient);
  private readonly ambiente = inject(AMBIENTE);

  postLogin(dto: PostLoginDto): Observable<PostLoginRespostaDto> {
    if (this.ambiente.usarMocks) return this.postLoginMock(dto);
    // Sem token (ainda não existe) e sem toast: a tela de login mostra o próprio erro.
    return this.http.post<PostLoginRespostaDto>('auth/login', dto, { context: semErro(semAuth()) });
  }

  private postLoginMock(dto: PostLoginDto): Observable<PostLoginRespostaDto> {
    const tokens = dto.senha === SENHA_DEMO ? emitirTokensMock(dto.email) : null;
    if (!tokens) return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' }));
    // delay simula latência para dar para ver o estado "entrando..." no botão.
    return of(tokens).pipe(delay(400));
  }
}

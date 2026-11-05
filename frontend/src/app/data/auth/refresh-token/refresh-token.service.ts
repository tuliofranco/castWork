import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { emailDoRefreshMock, emitirTokensMock } from '@data/auth/login/mocks/emitir-tokens.mock';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { semAuth, semErro } from '@shared/injection-tokens/http-context.tokens';
import { type Observable, of, throwError } from 'rxjs';

import type { PostRefreshTokenRespostaDto } from './dto/post-refresh-token-resposta.dto';
import type { PostRefreshTokenDto } from './dto/post-refresh-token.dto';

@Injectable({ providedIn: 'root' })
export class RefreshTokenService {
  private readonly http = inject(HttpClient);
  private readonly ambiente = inject(AMBIENTE);

  postRefreshToken(dto: PostRefreshTokenDto): Observable<PostRefreshTokenRespostaDto> {
    if (this.ambiente.usarMocks) {
      const tokens = emitirTokensMock(emailDoRefreshMock(dto.refreshToken) ?? '');
      return tokens ? of(tokens) : throwError(() => new HttpErrorResponse({ status: 401 }));
    }
    // semAuth: o refresh não pode passar pelo jwtInterceptor, senão um 401 aqui tentaria outro refresh (loop).
    return this.http.post<PostRefreshTokenRespostaDto>('auth/refresh', dto, { context: semErro(semAuth()) });
  }
}

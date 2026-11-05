import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { semAuth, semErro } from '@shared/injection-tokens/http-context.tokens';
import { type Observable, map, of } from 'rxjs';

import type { PostLogoutDto } from './dto/post-logout.dto';

@Injectable({ providedIn: 'root' })
export class LogoutService {
  private readonly http = inject(HttpClient);
  private readonly ambiente = inject(AMBIENTE);

  /** Revoga o refresh token no servidor. Sem toast: sair não pode falhar para o usuário. */
  postLogout(dto: PostLogoutDto): Observable<void> {
    if (this.ambiente.usarMocks) return of(undefined);
    return this.http.post('auth/logout', dto, { context: semErro(semAuth()) }).pipe(map(() => undefined));
  }
}

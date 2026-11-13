import { semErro } from '@shared/injection-tokens/http-context.tokens';
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { lerErroBlob } from '@shared/operators/ler-erro-blob.operator';
import { type Observable, delay, of } from 'rxjs';

import type { TipoDocumento } from './dto/tipo-documento';
import { criarPdfMock } from './mocks/pdf.mock';

/**
 * Contrato REST: GET documentos/pecas/{pecaId}/{tipo}.pdf → application/pdf.
 * Em erro, o corpo é ProblemDetails (JSON) — mas chega como Blob; `lerErroBlob` converte.
 */
@Injectable({ providedIn: 'root' })
export class DocumentoService {
  private readonly http = inject(HttpClient);
  private readonly ambiente = inject(AMBIENTE);

  getDocumentoPdf(pecaId: string, tipo: TipoDocumento): Observable<Blob> {
    if (this.ambiente.usarMocks) return of(criarPdfMock(`Castwork - ${tipo} da peca ${pecaId}`)).pipe(delay(300));
    return this.http
      .get(`documentos/pecas/${encodeURIComponent(pecaId)}/${tipo}.pdf`, { responseType: 'blob', context: semErro() })
      .pipe(lerErroBlob());
  }
}

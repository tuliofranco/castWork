import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { IdCriadoDto } from '@data/common/http/id-criado.dto';
import { type Observable, map } from 'rxjs';

import type { BriefingDto, PutFormularioBriefingDto } from './dto/briefing.dto';

/**
 * Briefing do projeto. Tudo por REST: a conversa e os PDFs não cabem bem no GraphQL.
 */
@Injectable({ providedIn: 'root' })
export class BriefingService {
  private readonly http = inject(HttpClient);

  /** Nulo quando o formulário ainda não foi preenchido (204). */
  getBriefing(projetoId: number): Observable<BriefingDto | null> {
    return this.http.get<BriefingDto | null>(`projetos/${projetoId}/briefing`);
  }

  putFormulario(projetoId: number, dto: PutFormularioBriefingDto): Observable<IdCriadoDto> {
    return this.http.put<IdCriadoDto>(`projetos/${projetoId}/briefing`, dto);
  }

  /** Sem texto, numa conversa vazia, a IA abre a conversa a partir do formulário. */
  postConversa(projetoId: number, texto: string | null): Observable<BriefingDto> {
    return this.http.post<BriefingDto>(`projetos/${projetoId}/briefing/conversa`, { texto });
  }

  postPdf(projetoId: number): Observable<IdCriadoDto> {
    return this.http.post<IdCriadoDto>(`projetos/${projetoId}/briefing/pdf`, {});
  }

  /** PDF com os dados atuais, sem guardar. */
  getPrevia(projetoId: number): Observable<Blob> {
    return this.http.get(`projetos/${projetoId}/briefing/pdf/previa`, { responseType: 'blob' });
  }

  getDocumento(projetoId: number, documentoId: number): Observable<Blob> {
    return this.http.get(`projetos/${projetoId}/briefing/documentos/${documentoId}`, { responseType: 'blob' }).pipe(map((blob) => blob));
  }
}

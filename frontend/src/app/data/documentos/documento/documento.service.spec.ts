import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AMBIENTE } from '@shared/injection-tokens/ambiente.token';
import { firstValueFrom } from 'rxjs';

import { DocumentoService } from './documento.service';

describe('DocumentoService', () => {
  function configurar(usarMocks: boolean) {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AMBIENTE, useValue: { production: false, apiUrl: '', graphqlUrl: '', usarMocks } },
      ],
    });
    return { service: TestBed.inject(DocumentoService), http: TestBed.inject(HttpTestingController) };
  }

  it('deve pedir o PDF como blob', async () => {
    const { service, http } = configurar(false);
    const promessa = firstValueFrom(service.getDocumentoPdf('42', 'orcamento'));
    const req = http.expectOne('documentos/pecas/42/orcamento.pdf');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['%PDF'], { type: 'application/pdf' }));
    expect((await promessa).type).toBe('application/pdf');
  });

  it('deve entregar o erro com o ProblemDetails já lido do Blob', async () => {
    const { service, http } = configurar(false);
    const promessa = firstValueFrom(service.getDocumentoPdf('42', 'certificado')).catch((e: unknown) => e);
    http
      .expectOne('documentos/pecas/42/certificado.pdf')
      .flush(new Blob([JSON.stringify({ title: 'Peça ainda não entregue' })]), { status: 409, statusText: 'Conflict' });
    const erro = (await promessa) as HttpErrorResponse;
    expect(erro.status).toBe(409);
    expect(erro.error).toEqual({ title: 'Peça ainda não entregue' });
  });

  it('em modo mock deve gerar um PDF válido', async () => {
    const { service } = configurar(true);
    const pdf = await firstValueFrom(service.getDocumentoPdf('42', 'orcamento'));
    expect(pdf.type).toBe('application/pdf');
    expect(await pdf.text()).toMatch(/^%PDF-1\.4[\s\S]*%%EOF$/);
  });
});

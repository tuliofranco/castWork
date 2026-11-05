import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { PaginacaoCursor } from '@shared/components/paginacao/paginacao-cursor';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs';

import { STATUS_PECA } from './status-peca';

const POR_PAGINA = 20;

@Component({
  selector: 'cw-pecas',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './pecas.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PecasComponent {
  private readonly pecasService = inject(PecasService);
  protected readonly podeCriar = inject(PermissaoService).temAlguma(['pecas.criar']);
  protected readonly status = STATUS_PECA;
  protected readonly paginacao = new PaginacaoCursor();

  protected readonly busca = new FormControl('', { nonNullable: true });

  // Espera o usuário parar de digitar; uma busca nova sempre volta para a primeira página.
  private readonly termo = toSignal(
    this.busca.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      tap(() => this.paginacao.reiniciar()),
    ),
    { initialValue: '' },
  );

  protected readonly pecas = rxResource({
    params: () => ({ termo: this.termo() || null, apos: this.paginacao.apos(), primeiros: POR_PAGINA }),
    stream: ({ params }) => this.pecasService.getPecas(params),
  });
}

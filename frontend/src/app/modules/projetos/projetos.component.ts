import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { PaginacaoCursor } from '@shared/components/paginacao/paginacao-cursor';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs';

import { STATUS_VERSAO } from './status-versao';

const POR_PAGINA = 20;

@Component({
  selector: 'cw-projetos',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './projetos.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjetosComponent {
  private readonly projetosService = inject(ProjetosService);
  protected readonly podeCriar = inject(PermissaoService).temAlguma(['projetos.criar']);
  protected readonly paginacao = new PaginacaoCursor();
  protected readonly status = STATUS_VERSAO;

  protected readonly busca = new FormControl('', { nonNullable: true });

  private readonly termo = toSignal(
    this.busca.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      tap(() => this.paginacao.reiniciar()),
    ),
    { initialValue: '' },
  );

  protected readonly projetos = rxResource({
    params: () => ({ termo: this.termo() || null, apos: this.paginacao.apos(), primeiros: POR_PAGINA }),
    stream: ({ params }) => this.projetosService.getProjetos(params),
  });
}

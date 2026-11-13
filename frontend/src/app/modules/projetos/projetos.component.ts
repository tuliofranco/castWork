import { valorDe } from '@shared/recursos/valor-de';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import type { ProjetoResumoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { PaginacaoCursor } from '@shared/components/paginacao/paginacao-cursor';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoBuscaComponent } from '@shared/ui/campo-busca.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { PaginacaoComponent } from '@shared/ui/paginacao.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs';

import { STATUS_VERSAO } from './status-versao';

const POR_PAGINA = 20;

@Component({
  selector: 'cw-projetos',
  imports: [RouterLink, CabecalhoPaginaComponent, CampoBuscaComponent, EstadoListaComponent, PaginacaoComponent, SeloComponent],
  templateUrl: './projetos.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjetosComponent {
  protected readonly valorDe = valorDe;
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

  /** Versões que contam para a lista (as arquivadas ficam de fora), da mais recente para a mais antiga. */
  protected versoesAtivas(projeto: ProjetoResumoDto): ProjetoResumoDto['versoes'] {
    return projeto.versoes.filter((v) => v.status !== 'ARQUIVADA');
  }
}

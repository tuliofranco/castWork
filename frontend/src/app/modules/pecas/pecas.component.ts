import { valorDe } from '@shared/recursos/valor-de';
import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { PaginacaoCursor } from '@shared/components/paginacao/paginacao-cursor';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoBuscaComponent } from '@shared/ui/campo-busca.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { PaginacaoComponent } from '@shared/ui/paginacao.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { formatarTeor, iconeTipoPeca, rotuloTipoPeca } from '@shared/pecas/tipo-peca';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs';

const POR_PAGINA = 20;

/**
 * Biblioteca de peças: só busca. Sem "Nova peça" nem edição (a peça chega aqui ao ser publicada num projeto) e sem
 * valor (o valor é de cada projeto). Cada peça abre o detalhe de consulta, com o histórico de uso.
 */
@Component({
  selector: 'cw-pecas',
  imports: [DecimalPipe, RouterLink, CabecalhoPaginaComponent, CampoBuscaComponent, EstadoListaComponent, PaginacaoComponent, SeloComponent],
  templateUrl: './pecas.component.html',
  styles: `
    .peso { color: var(--cw-ink-muted); font-variant-numeric: tabular-nums; white-space: nowrap; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PecasComponent {
  protected readonly valorDe = valorDe;
  private readonly pecasService = inject(PecasService);
  protected readonly paginacao = new PaginacaoCursor();
  protected readonly rotuloTipo = rotuloTipoPeca;
  protected readonly iconeTipo = iconeTipoPeca;
  protected readonly formatarTeor = formatarTeor;

  protected readonly busca = new FormControl('', { nonNullable: true });

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

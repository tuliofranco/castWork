import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { formatarTeor, iconeTipoPeca, rotuloTipoPeca } from '@shared/pecas/tipo-peca';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { SeloComponent } from '@shared/ui/selo.component';

/**
 * Detalhe de consulta de uma peça da biblioteca: os dados, o desenho (2D e 3D, copiados ao publicar, abertos só para ver
 * em `pecas/:id/desenhos/:ferramenta`) e o histórico de uso (projeto, versões e o valor em cada projeto). Não edita nada.
 * O histórico só é pedido com `projetos.ler`; o valor, com `precificacao.ler`.
 */
@Component({
  selector: 'cw-peca-detalhe',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, RouterLink, CabecalhoPaginaComponent, EstadoListaComponent, SeloComponent],
  templateUrl: './peca-detalhe.component.html',
  styles: `
    .dados { display: grid; grid-template-columns: repeat(auto-fill, minmax(10rem, 1fr)); gap: 1rem 1.5rem; margin: 0 0 2rem; }
    .dados dt { color: var(--cw-ink-muted); font-size: 0.8125rem; font-weight: 600; }
    .dados dd { margin: 0.125rem 0 0; font-variant-numeric: tabular-nums; }
    .descricao { grid-column: 1 / -1; white-space: pre-line; }
    h2 { margin: 0 0 0.75rem; font-size: 1.0625rem; font-weight: 600; }
    .uso { align-items: flex-start; }
    .versoes { display: flex; flex-wrap: wrap; gap: 0.25rem 0.75rem; margin: 0.25rem 0 0; padding: 0; list-style: none; color: var(--cw-ink-muted); font-size: 0.875rem; }
    .valor { font-variant-numeric: tabular-nums; font-weight: 600; white-space: nowrap; min-width: 6.5rem; text-align: right; }
    .sem-uso { color: var(--cw-ink-muted); }
    .desenho { margin: 0 0 2rem; }
    /* Mesmo cartão das ferramentas do projeto. */
    .lista-ferramentas { display: flex; flex-wrap: wrap; gap: 0.75rem; }
    .ferramenta {
      display: flex; align-items: center; gap: 0.75rem; min-height: 3.5rem; padding: 0.625rem 1rem 0.625rem 0.75rem;
      border: 1px solid var(--cw-border); border-radius: var(--cw-radius-lg); color: var(--cw-ink); text-decoration: none;
    }
    .ferramenta > .bi {
      display: grid; place-items: center; width: 2.5rem; height: 2.5rem; border-radius: var(--cw-radius);
      background: var(--cw-accent-soft); color: var(--cw-accent-ink); font-size: 1.125rem;
    }
    .ferramenta span { display: flex; flex-direction: column; }
    .ferramenta small { color: var(--cw-ink-muted); font-size: 0.8125rem; }
    .ferramenta:hover { background: var(--cw-surface-hover); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PecaDetalheComponent {
  private readonly pecasService = inject(PecasService);
  private readonly permissoes = inject(PermissaoService);

  /** `:id` da rota (withComponentInputBinding). */
  readonly id = input.required<string>();

  /** O histórico diz em que projetos a peça está: exige ler projetos. */
  protected readonly podeVerHistorico = this.permissoes.temAlguma(['projetos.ler']);
  /** O valor em cada projeto é dado de precificação. */
  protected readonly podeVerValor = this.permissoes.temAlguma(['precificacao.ler']);
  protected readonly rotuloTipo = rotuloTipoPeca;
  protected readonly iconeTipo = iconeTipoPeca;
  protected readonly formatarTeor = formatarTeor;

  protected nomeDaFerramenta(ferramenta: string): string {
    const nomes: Record<string, string> = { 'modelo-3d': 'Modelo 3D', 'desenho-2d': 'Desenho 2D' };
    return nomes[ferramenta] ?? ferramenta;
  }

  protected readonly peca = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.pecasService.getPeca(params, { comHistorico: this.podeVerHistorico, comValor: this.podeVerValor }),
  });
}

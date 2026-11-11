import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { PermissaoService } from '@core/services/permissao.service';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';

import { CotacaoMetalComponent } from './cotacao-metal.component';
import { LigasAtelieComponent } from './ligas-atelie.component';
import { ParametrosPrecoComponent } from './parametros-preco.component';

/**
 * Preços do ateliê: cotação dos metais (com histórico), ligas e parâmetros que entram em todo orçamento.
 * A rota exige `precificacao.ler`; sem `precificacao.editar`, a tela é só leitura.
 */
@Component({
  selector: 'cw-precificacao',
  imports: [CabecalhoPaginaComponent, EstadoListaComponent, CotacaoMetalComponent, LigasAtelieComponent, ParametrosPrecoComponent],
  templateUrl: './precificacao.component.html',
  styleUrl: './precificacao.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrecificacaoComponent {
  private readonly precosService = inject(PrecosService);

  protected readonly podeEditar = inject(PermissaoService).temAlguma(['precificacao.editar']);

  protected readonly precos = rxResource({ stream: () => this.precosService.getPrecos() });
}

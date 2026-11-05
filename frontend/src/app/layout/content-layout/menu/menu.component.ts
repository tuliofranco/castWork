import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import type { CastworkRoute } from '@core/models/rota-data.model';
import { PermissaoService } from '@core/services/permissao.service';

import { montarMenu } from './montar-menu';

@Component({
  selector: 'cw-menu',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './menu.component.html',
  styleUrl: './menu.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MenuComponent {
  private readonly rota = inject(ActivatedRoute);
  private readonly permissao = inject(PermissaoService);

  // O menu lê as filhas da rota que renderiza o layout: as rotas são a fonte do menu.
  // temAlguma lê o signal de permissões, então o computed recalcula se o usuário mudar.
  protected readonly itens = computed(() =>
    montarMenu((this.rota.routeConfig?.children ?? []) as CastworkRoute[], (p) => this.permissao.temAlguma(p)),
  );
}

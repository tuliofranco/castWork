import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

import { MenuComponent } from './menu/menu.component';
import { UsuarioComponent } from './usuario/usuario.component';

/**
 * Casca do app, tablet-first:
 * - 768–1199 px: rail de 88 px (ícone com rótulo embaixo), sempre visível, sem botão de abrir;
 * - ≥ 1200 px: rail expandido com rótulo ao lado;
 * - < 768 px: barra inferior de navegação e topo compacto com a marca e o usuário.
 */
@Component({
  selector: 'cw-content-layout',
  imports: [RouterOutlet, RouterLink, MenuComponent, UsuarioComponent],
  templateUrl: './content-layout.component.html',
  styleUrl: './content-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContentLayoutComponent {}

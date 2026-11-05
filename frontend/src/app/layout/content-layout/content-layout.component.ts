import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { HeaderComponent } from './header/header.component';
import { MenuComponent } from './menu/menu.component';

@Component({
  selector: 'cw-content-layout',
  imports: [RouterOutlet, HeaderComponent, MenuComponent],
  templateUrl: './content-layout.component.html',
  styleUrl: './content-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContentLayoutComponent {
  /** Só tem efeito abaixo de `lg`; no desktop o menu é sempre visível. */
  protected readonly menuAberto = signal(false);

  protected alternarMenu(): void {
    this.menuAberto.update((aberto) => !aberto);
  }

  protected fecharMenu(): void {
    this.menuAberto.set(false);
  }
}

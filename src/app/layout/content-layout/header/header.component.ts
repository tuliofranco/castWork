import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { AuthService } from '@core/services/auth.service';
import { SessaoService } from '@core/services/sessao.service';

@Component({
  selector: 'cw-header',
  templateUrl: './header.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent {
  private readonly auth = inject(AuthService);
  protected readonly sessao = inject(SessaoService);

  readonly alternarMenu = output();

  protected sair(): void {
    this.auth.logout();
  }
}

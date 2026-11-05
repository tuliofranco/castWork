import { ChangeDetectionStrategy, Component, output } from '@angular/core';

@Component({
  selector: 'cw-header',
  templateUrl: './header.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent {
  readonly alternarMenu = output();
}

import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'cw-error-layout',
  imports: [RouterOutlet],
  template: `
    <main class="d-flex vh-100 align-items-center justify-content-center bg-body-tertiary">
      <router-outlet />
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorLayoutComponent {}

import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from '@shared/components/toast-container/toast-container.component';

@Component({
  selector: 'cw-root',
  imports: [RouterOutlet, ToastContainerComponent],
  template: '<router-outlet /><cw-toast-container />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}

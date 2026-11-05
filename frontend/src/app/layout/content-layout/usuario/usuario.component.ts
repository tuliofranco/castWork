import { ChangeDetectionStrategy, Component, ElementRef, inject, input, signal } from '@angular/core';
import { AuthService } from '@core/services/auth.service';
import { SessaoService } from '@core/services/sessao.service';
import { IniciaisComponent } from '@shared/ui/iniciais.component';

/**
 * Usuário logado: iniciais no rail e, ao tocar, um painel com nome, ateliê e "Sair".
 * Fecha ao tocar fora ou com Esc. `posicao` define para onde o painel abre (lado do rail ou abaixo, no celular).
 */
@Component({
  selector: 'cw-usuario',
  imports: [IniciaisComponent],
  templateUrl: './usuario.component.html',
  styleUrl: './usuario.component.scss',
  host: {
    '[class]': '"posicao-" + posicao()',
    '(document:click)': 'aoClicarFora($event)',
    '(document:keydown.escape)': 'aberto.set(false)',
  },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsuarioComponent {
  private readonly auth = inject(AuthService);
  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);
  protected readonly sessao = inject(SessaoService);

  readonly posicao = input<'lateral' | 'abaixo'>('lateral');

  protected readonly aberto = signal(false);

  protected alternar(): void {
    this.aberto.update((v) => !v);
  }

  protected sair(): void {
    this.aberto.set(false);
    this.auth.logout();
  }

  protected aoClicarFora(evento: MouseEvent): void {
    if (this.aberto() && !this.elemento.nativeElement.contains(evento.target as Node)) this.aberto.set(false);
  }
}

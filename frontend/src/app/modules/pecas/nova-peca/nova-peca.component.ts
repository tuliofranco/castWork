import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { ToastService } from '@shared/services/toast.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'cw-nova-peca',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './nova-peca.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NovaPecaComponent {
  private readonly pecasService = inject(PecasService);
  private readonly clientesService = inject(ClientesService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly clientes = rxResource({ stream: () => this.clientesService.getClientesOpcoes() });

  protected readonly form = inject(FormBuilder).nonNullable.group({
    clienteId: [null as number | null, Validators.required],
    titulo: ['', [Validators.required, Validators.maxLength(150)]],
    descricao: ['', Validators.maxLength(4000)],
    prazoEntrega: [''],
  });

  protected readonly enviando = signal(false);
  protected readonly errosGerais = signal<readonly string[]>([]);

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { clienteId, titulo, descricao, prazoEntrega } = this.form.getRawValue();
    if (clienteId === null) return;
    this.enviando.set(true);
    this.errosGerais.set([]);

    this.pecasService
      .postPeca({ clienteId, titulo, descricao: descricao || null, prazoEntrega: prazoEntrega || null })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          this.toast.sucesso('Peça criada em briefing.');
          void this.router.navigate(['/pecas']);
        },
        error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
      });
  }
}

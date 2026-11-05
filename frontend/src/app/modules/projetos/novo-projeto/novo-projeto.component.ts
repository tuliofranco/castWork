import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { ToastService } from '@shared/services/toast.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'cw-novo-projeto',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './novo-projeto.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NovoProjetoComponent {
  private readonly projetosService = inject(ProjetosService);
  private readonly clientesService = inject(ClientesService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly clientes = rxResource({ stream: () => this.clientesService.getClientesOpcoes() });

  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    descricao: ['', Validators.maxLength(2000)],
    clienteId: [null as number | null],
  });

  protected readonly enviando = signal(false);
  protected readonly errosGerais = signal<readonly string[]>([]);

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { nome, descricao, clienteId } = this.form.getRawValue();
    this.enviando.set(true);
    this.errosGerais.set([]);

    this.projetosService
      .postProjeto({ nome, descricao: descricao || null, clienteId })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: ({ id }) => {
          this.toast.sucesso('Projeto criado com a Versão 1.');
          void this.router.navigate(['/projetos', id]);
        },
        error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
      });
  }
}

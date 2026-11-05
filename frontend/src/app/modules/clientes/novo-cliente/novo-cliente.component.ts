import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { BarraAcoesComponent } from '@shared/ui/barra-acoes.component';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoComponent } from '@shared/ui/campo.component';

import { ToastService } from '@shared/services/toast.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'cw-novo-cliente',
  imports: [ReactiveFormsModule, RouterLink, CabecalhoPaginaComponent, CampoComponent, BarraAcoesComponent],
  templateUrl: './novo-cliente.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NovoClienteComponent {
  private readonly clientesService = inject(ClientesService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  // CPF/CNPJ: a validação dos dígitos é do servidor (inclusive CNPJ alfanumérico); aqui só o obrigatório e o tamanho.
  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    documento: [''],
    email: ['', Validators.email],
    telefone: ['', Validators.maxLength(20)],
  });

  protected readonly enviando = signal(false);
  protected readonly errosGerais = signal<readonly string[]>([]);

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { nome, documento, email, telefone } = this.form.getRawValue();
    this.enviando.set(true);
    this.errosGerais.set([]);

    this.clientesService
      .postCliente({ nome, documento: documento || null, email: email || null, telefone: telefone || null })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          this.toast.sucesso('Cliente cadastrado.');
          void this.router.navigate(['/clientes']);
        },
        error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
      });
  }
}

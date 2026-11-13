import { PermissaoService } from '@core/services/permissao.service';
import { valorDe } from '@shared/recursos/valor-de';
import { ChangeDetectionStrategy, Component, inject, signal, computed } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { BarraAcoesComponent } from '@shared/ui/barra-acoes.component';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoComponent } from '@shared/ui/campo.component';

import { ToastService } from '@shared/services/toast.service';
import { finalize, of } from 'rxjs';

@Component({
  selector: 'cw-novo-projeto',
  imports: [ReactiveFormsModule, RouterLink, CabecalhoPaginaComponent, CampoComponent, BarraAcoesComponent],
  templateUrl: './novo-projeto.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NovoProjetoComponent {
  protected readonly valorDe = valorDe;
  private readonly projetosService = inject(ProjetosService);
  private readonly clientesService = inject(ClientesService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  /** `clientes.ler`: sem a regra, a lista de clientes não é pedida e o projeto nasce sem cliente. */
  protected readonly podeVerClientes = inject(PermissaoService).temAlguma(['clientes.ler']);
  protected readonly clientes = rxResource({ stream: () => (this.podeVerClientes ? this.clientesService.getClientesOpcoes() : of([])) });

  /** A consulta traz no máximo 50 clientes: com a lista cheia, pode haver cliente fora dela. */
  protected readonly listaCortada = computed(() => (valorDe(this.clientes)?.length ?? 0) >= 50);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    descricao: ['', Validators.maxLength(2000)],
    clienteId: [{ value: null as number | null, disabled: !this.podeVerClientes }],
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
          this.toast.sucesso('Projeto criado.');
          void this.router.navigate(['/projetos', id]);
        },
        error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
      });
  }
}

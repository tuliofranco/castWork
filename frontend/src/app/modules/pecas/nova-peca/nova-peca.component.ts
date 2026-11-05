import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { BarraAcoesComponent } from '@shared/ui/barra-acoes.component';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoComponent } from '@shared/ui/campo.component';

import { TIPOS_PECA, type TipoPeca } from '@shared/pecas/tipo-peca';
import { ToastService } from '@shared/services/toast.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'cw-nova-peca',
  imports: [ReactiveFormsModule, RouterLink, CabecalhoPaginaComponent, CampoComponent, BarraAcoesComponent],
  templateUrl: './nova-peca.component.html',
  // Tipo como botões de escolha grandes: um toque, sem abrir lista no tablet.
  styles: `
    .tipos { border: 0; padding: 0; margin: 0 0 1.5rem; }
    .opcoes { display: flex; flex-wrap: wrap; gap: 0.5rem; }
    .opcao {
      display: inline-flex; align-items: center; gap: 0.5rem; min-height: var(--cw-alvo); padding: 0 1rem;
      border: 1px solid var(--cw-border-strong); border-radius: var(--cw-radius); background: var(--cw-surface);
      font-weight: 600; cursor: pointer; transition: background-color var(--cw-rapido) var(--cw-ease), border-color var(--cw-rapido) var(--cw-ease);
    }
    .opcao:hover { background: var(--cw-surface-hover); }
    .opcao:has(input:focus-visible) { box-shadow: var(--cw-focus); }
    .opcao.marcada { border-color: var(--cw-accent); background: var(--cw-accent-soft); color: var(--cw-accent-ink); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NovaPecaComponent {
  private readonly pecasService = inject(PecasService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly tipos = TIPOS_PECA;

  // Teor e peso são validados no servidor (aceita "18k" ou "750"); aqui só o essencial.
  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    tipo: ['ELO' as TipoPeca, Validators.required],
    material: ['', Validators.maxLength(60)],
    teor: [''],
    pesoGramas: [null as number | null, Validators.min(0.001)],
    descricao: ['', Validators.maxLength(2000)],
  });

  protected readonly enviando = signal(false);
  protected readonly errosGerais = signal<readonly string[]>([]);

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const valor = this.form.getRawValue();
    this.enviando.set(true);
    this.errosGerais.set([]);

    this.pecasService
      .postPeca({
        nome: valor.nome,
        tipo: valor.tipo,
        material: valor.material || null,
        teor: valor.teor || null,
        pesoGramas: valor.pesoGramas,
        descricao: valor.descricao || null,
      })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          this.toast.sucesso('Peça cadastrada na biblioteca.');
          void this.router.navigate(['/pecas']);
        },
        error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
      });
  }
}

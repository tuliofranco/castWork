import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import type { PecaDetalheDto } from '@data/pecas/pecas/dto/peca.dto';
import type { PostPecaDto } from '@data/pecas/pecas/dto/post-peca.dto';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { BarraAcoesComponent } from '@shared/ui/barra-acoes.component';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoComponent } from '@shared/ui/campo.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';

import { TIPOS_PECA, type TipoPeca } from '@shared/pecas/tipo-peca';
import { ToastService } from '@shared/services/toast.service';
import { type Observable, finalize, of } from 'rxjs';

/**
 * Cadastro e edição de uma peça da biblioteca (rota `nova` ou `:id/editar`). O custo por unidade só aparece
 * para quem tem `precificacao.ler`: quem não vê o custo também não o altera (o servidor mantém o atual).
 */
@Component({
  selector: 'cw-nova-peca',
  imports: [ReactiveFormsModule, RouterLink, CabecalhoPaginaComponent, CampoComponent, BarraAcoesComponent, EstadoListaComponent],
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

  /** `:id` da rota de edição (withComponentInputBinding); ausente no cadastro. */
  readonly id = input<string>();

  protected readonly tipos = TIPOS_PECA;
  protected readonly podeVerCusto = inject(PermissaoService).temAlguma(['precificacao.ler']);
  protected readonly editando = computed(() => !!this.id());

  // Teor e peso são validados no servidor (aceita "18k" ou "750"); aqui só o essencial.
  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    tipo: ['ELO' as TipoPeca, Validators.required],
    material: ['', Validators.maxLength(60)],
    teor: [''],
    pesoGramas: [null as number | null, Validators.min(0.001)],
    custo: [null as number | null, Validators.min(0)],
    descricao: ['', Validators.maxLength(2000)],
  });

  protected readonly peca = rxResource({
    params: () => (this.id() ? Number(this.id()) : null),
    stream: ({ params }) => (params === null ? of(null) : this.pecasService.getPeca(params, this.podeVerCusto)),
  });

  protected readonly enviando = signal(false);
  protected readonly errosGerais = signal<readonly string[]>([]);

  constructor() {
    // Na edição, o formulário começa com os dados salvos.
    effect(() => {
      const peca = this.peca.value();
      if (peca) this.preencher(peca);
    });
  }

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const valor = this.form.getRawValue();
    const dto: PostPecaDto = {
      nome: valor.nome,
      tipo: valor.tipo,
      material: valor.material || null,
      teor: valor.teor || null,
      pesoGramas: valor.pesoGramas,
      descricao: valor.descricao || null,
      ...(this.podeVerCusto ? { custo: valor.custo } : {}),
    };
    const id = this.id();
    const envio: Observable<unknown> = id ? this.pecasService.putPeca(Number(id), dto) : this.pecasService.postPeca(dto);
    this.enviando.set(true);
    this.errosGerais.set([]);

    envio.pipe(finalize(() => this.enviando.set(false))).subscribe({
      next: () => {
        this.toast.sucesso(id ? 'Peça alterada.' : 'Peça cadastrada na biblioteca.');
        void this.router.navigate(['/pecas']);
      },
      error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
    });
  }

  private preencher(peca: PecaDetalheDto): void {
    this.form.reset({
      nome: peca.nome,
      tipo: peca.tipo,
      material: peca.material ?? '',
      teor: peca.teor === null ? '' : String(peca.teor),
      pesoGramas: peca.pesoGramas,
      custo: peca.custo ?? null,
      descricao: peca.descricao ?? '',
    });
  }
}

import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import type { PecaDto } from '@data/pecas/pecas/dto/peca.dto';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import type { PecaDoProjetoDto } from '@data/projetos/projetos/dto/peca-do-projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { formatarTeor, iconeTipoPeca, rotuloTipoPeca } from '@shared/pecas/tipo-peca';
import { ToastService } from '@shared/services/toast.service';
import { CampoBuscaComponent } from '@shared/ui/campo-busca.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { type Observable, debounceTime, distinctUntilChanged, finalize, of } from 'rxjs';

import { STATUS_PECA_DO_PROJETO } from '../status-peca-do-projeto';

/**
 * Peças do projeto: as que a composição das versões pode usar. Cria (rascunho), importa da biblioteca (com busca),
 * abre para editar, define o valor e publica na biblioteca. O valor só aparece com `precificacao.ler` e só se define
 * com `precificacao.editar`; criar, importar e publicar pedem `projetos.editar` (a busca da biblioteca, `pecas.ler`).
 */
@Component({
  selector: 'cw-pecas-projeto',
  imports: [CurrencyPipe, ReactiveFormsModule, RouterLink, CampoBuscaComponent, EstadoListaComponent, SeloComponent],
  template: `
    <section class="secao" aria-labelledby="titulo-pecas-projeto">
      <div class="cabecalho-secao">
        <h2 id="titulo-pecas-projeto">Peças do projeto</h2>
        @if (podeEditar()) {
          @if (podeImportar()) {
            <button type="button" class="btn btn-sutil" [attr.aria-pressed]="importando()" (click)="importando.set(!importando())">
              <i class="bi bi-box-arrow-in-down" aria-hidden="true"></i> Importar da biblioteca
            </button>
          }
          <a class="btn btn-primary" routerLink="pecas/nova"><i class="bi bi-plus-lg" aria-hidden="true"></i> Nova peça</a>
        }
      </div>

      @if (importando()) {
        <div class="importar">
          <cw-campo-busca [controle]="busca" placeholder="Nome ou material" rotulo="Buscar na biblioteca" />
          @let resultados = biblioteca.value();
          <cw-estado-lista
            [carregando]="!biblioteca.hasValue() && biblioteca.isLoading()"
            [erro]="!!biblioteca.error()"
            mensagemErro="Não foi possível buscar na biblioteca."
            [vazio]="resultados?.nodes?.length === 0"
            iconeVazio="gem"
            [tituloVazio]="busca.value ? 'Nenhuma peça encontrada.' : 'A biblioteca está vazia.'"
            [quantidadeEsqueleto]="2"
            (tentarDeNovo)="biblioteca.reload()"
          >
            @if (resultados) {
              <ul class="cw-lista">
                @for (peca of resultados.nodes; track peca.id) {
                  <li class="cw-linha resultado">
                    <span class="principal">
                      <span class="nome">{{ peca.nome }}</span>
                      <span class="meta">{{ meta(peca) }}</span>
                    </span>
                    @if (importadas().has(peca.id)) {
                      <cw-selo icone="check2">No projeto</cw-selo>
                    } @else {
                      <button type="button" class="btn btn-secundario" [disabled]="salvando()" (click)="importar(peca)">Importar</button>
                    }
                  </li>
                }
              </ul>
            }
          </cw-estado-lista>
        </div>
      }

      @if (pecas.error()) {
        <p class="falha" role="alert">Não foi possível carregar as peças do projeto.</p>
      } @else if (pecas.value(); as lista) {
        @if (lista.length === 0) {
          <p class="vazia">Nenhuma peça ainda. Crie uma peça ou importe da biblioteca para usar nas versões.</p>
        } @else {
          <ul class="cw-lista">
            @for (p of lista; track p.id) {
              <li class="cw-linha peca" [class.inativa]="!p.ativo">
                <a class="abrir" [routerLink]="['pecas', p.id]" [attr.aria-label]="'Abrir ' + p.nome">
                  <span class="cw-icone-linha"><i class="bi" [class]="'bi-' + iconeTipo(p.tipo)" aria-hidden="true"></i></span>
                  <span class="principal">
                    <span class="nome">{{ p.nome }}</span>
                    <span class="meta">{{ meta(p) }}</span>
                  </span>
                  <cw-selo [tom]="status[p.status].tom" [icone]="status[p.status].icone">{{ status[p.status].rotulo }}</cw-selo>
                  @if (!p.ativo) {
                    <cw-selo icone="pause-circle">Inativa</cw-selo>
                  }
                  @if (podeVerValor()) {
                    @if (p.valor !== null && p.valor !== undefined) {
                      <span class="valor" title="Valor no projeto (custo por unidade)">{{ p.valor | currency: 'BRL' }}</span>
                    } @else {
                      <cw-selo class="sem-valor" tom="aviso" icone="exclamation-circle">Sem valor</cw-selo>
                    }
                  }
                </a>
                <span class="acoes">
                  @if (podeEditarValor()) {
                    <button type="button" class="btn btn-sutil" [disabled]="salvando()" (click)="abrirValor(p)">Definir valor</button>
                  }
                  @if (podeEditar() && p.status === 'RASCUNHO' && p.ativo) {
                    <button type="button" class="btn btn-secundario" [disabled]="salvando()" (click)="publicar(p)">
                      <i class="bi bi-cloud-upload" aria-hidden="true"></i> Publicar
                    </button>
                  }
                </span>
                @if (definindoValor() === p.id) {
                  <form class="definir-valor" [formGroup]="formValor" (ngSubmit)="salvarValor(p)" novalidate>
                    <label class="visually-hidden" [for]="'valor-' + p.id">Valor de {{ p.nome }} no projeto (R$)</label>
                    <input [id]="'valor-' + p.id" type="number" step="0.01" min="0" inputmode="decimal" class="form-control" placeholder="R$ por unidade" formControlName="valor" />
                    <button type="submit" class="btn btn-primary" [disabled]="salvando() || valor.invalid">Salvar</button>
                    <button type="button" class="btn btn-sutil" (click)="definindoValor.set(null)">Cancelar</button>
                    @if (valor.invalid) {
                      <span class="erro" role="alert">O valor não pode ser negativo.</span>
                    }
                  </form>
                }
              </li>
            }
          </ul>
        }
      } @else {
        <p class="carregando" aria-busy="true">Carregando as peças...</p>
      }
    </section>
  `,
  styles: `
    .secao { margin-bottom: 2.25rem; }
    .cabecalho-secao { display: flex; flex-wrap: wrap; align-items: center; gap: 0.5rem; margin-bottom: 0.75rem; }
    h2 { margin: 0 auto 0 0; font-size: 1.0625rem; font-weight: 600; }
    .importar { margin: 0 0 1rem; padding: 1rem; border: 1px solid var(--cw-border); border-radius: var(--cw-radius-lg); }
    .peca { flex-wrap: wrap; }
    .abrir { display: flex; flex: 1 1 18rem; align-items: center; gap: 0.75rem; min-width: 0; color: inherit; text-decoration: none; }
    .acoes { display: flex; gap: 0.5rem; }
    .valor { font-variant-numeric: tabular-nums; font-weight: 600; white-space: nowrap; min-width: 6.5rem; text-align: right; }
    .definir-valor { display: flex; flex: 1 1 100%; flex-wrap: wrap; align-items: center; gap: 0.5rem; }
    .definir-valor input { max-width: 12rem; }
    .erro { color: var(--cw-danger); font-size: 0.875rem; }
    .vazia, .carregando, .falha { margin: 0; color: var(--cw-ink-muted); }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PecasProjetoComponent {
  private readonly projetosService = inject(ProjetosService);
  private readonly pecasService = inject(PecasService);
  private readonly toast = inject(ToastService);

  readonly projetoId = input.required<number>();
  /** `projetos.editar`: criar, importar, editar e publicar. */
  readonly podeEditar = input(false);
  /** `pecas.ler`: buscar na biblioteca para importar. */
  readonly podeImportar = input(false);
  /** `precificacao.ler`: ver o valor de cada peça. */
  readonly podeVerValor = input(false);
  /** `precificacao.editar`: definir o valor. */
  readonly podeEditarValor = input(false);
  /** Alguma peça mudou (importada, publicada...): a página relê o projeto (a publicação da versão depende do status). */
  readonly alterado = output<void>();

  protected readonly status = STATUS_PECA_DO_PROJETO;
  protected readonly iconeTipo = iconeTipoPeca;
  protected readonly salvando = signal(false);
  protected readonly importando = signal(false);
  protected readonly definindoValor = signal<number | null>(null);
  protected readonly formValor = new FormGroup({ valor: new FormControl<number | null>(null, Validators.min(0)) });
  protected readonly valor = this.formValor.controls.valor;

  protected readonly pecas = rxResource({
    params: () => ({ id: this.projetoId(), comValor: this.podeVerValor() }),
    stream: ({ params }) => this.projetosService.getPecasDoProjeto(params.id, params.comValor),
  });

  /** Peças da biblioteca que já têm cópia vinculada neste projeto (não se importa duas vezes). */
  protected readonly importadas = computed(() => new Set((this.pecas.value() ?? []).map((p) => p.pecaId).filter((id): id is number => id !== null)));

  protected readonly busca = new FormControl('', { nonNullable: true });
  private readonly termo = toSignal(this.busca.valueChanges.pipe(debounceTime(300), distinctUntilChanged()), { initialValue: '' });
  protected readonly biblioteca = rxResource({
    params: () => ({ termo: this.termo(), aberto: this.importando() }),
    stream: ({ params }) => (params.aberto ? this.pecasService.getPecas({ termo: params.termo || null, primeiros: 8, somenteAtivas: true }) : of(null)),
  });

  protected meta(peca: Pick<PecaDto, 'tipo' | 'material' | 'teor'>): string {
    return [rotuloTipoPeca(peca.tipo), peca.material, peca.teor ? formatarTeor(peca.teor) : null].filter(Boolean).join(' · ');
  }

  protected importar(peca: PecaDto): void {
    this.executar(this.projetosService.postImportarPeca(this.projetoId(), peca.id), `${peca.nome} importada da biblioteca.`);
  }

  protected publicar(peca: PecaDoProjetoDto): void {
    this.executar(this.projetosService.postPublicarPeca(this.projetoId(), peca.id), `${peca.nome} publicada na biblioteca.`);
  }

  protected abrirValor(peca: PecaDoProjetoDto): void {
    this.valor.reset(peca.valor ?? null);
    this.definindoValor.set(peca.id);
  }

  protected salvarValor(peca: PecaDoProjetoDto): void {
    if (this.valor.invalid) return;
    const valor = this.valor.value;
    this.executar(this.projetosService.putValorDaPeca(this.projetoId(), peca.id, { valor }), valor === null ? 'Valor retirado.' : 'Valor definido.', () =>
      this.definindoValor.set(null),
    );
  }

  /** Executa o command e relê as peças (erros 400/422 já viram toast no interceptor). */
  private executar(comando: Observable<unknown>, sucesso: string, depois?: () => void): void {
    this.salvando.set(true);
    comando.pipe(finalize(() => this.salvando.set(false))).subscribe({
      next: () => {
        this.toast.sucesso(sucesso);
        depois?.();
        this.pecas.reload();
        this.alterado.emit();
      },
      error: () => this.pecas.reload(),
    });
  }
}

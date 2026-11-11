import { valorDe } from '@shared/recursos/valor-de';
import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import type { CotacaoVigenteDto } from '@data/precificacao/precos/dto/precos.dto';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import { PaginacaoCursor } from '@shared/components/paginacao/paginacao-cursor';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { ToastService } from '@shared/services/toast.service';
import { PaginacaoComponent } from '@shared/ui/paginacao.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { finalize } from 'rxjs';

import { DIAS_PARA_DESATUALIZAR, diasDesde, haQuanto, metalPuro, moeda, rotuloMetal } from './formatos';

const POR_PAGINA = 10;

/**
 * Cotação vigente de um metal (R$/g do metal puro), com a idade dela, o botão de atualizar e o histórico.
 * Atualizar não apaga nada: cada valor vira uma linha nova no histórico.
 */
@Component({
  selector: 'cw-cotacao-metal',
  imports: [DatePipe, ReactiveFormsModule, PaginacaoComponent, SeloComponent],
  template: `
    @let atual = cotacao().atual;
    <article class="cartao" [attr.aria-labelledby]="'titulo-' + id()">
      <h3 [id]="'titulo-' + id()">{{ rotulo() }} <span class="puro">{{ puro() }}</span></h3>
      @if (atual) {
        <p class="valor">{{ moeda(atual.valor) }}<span class="unidade">/g</span></p>
        <p class="quando">
          Atualizada {{ haQuanto(dias()!) }}
          @if (atual.registradaPor) {
            por {{ atual.registradaPor }}
          }
        </p>
        @if (desatualizada()) {
          <cw-selo tom="aviso" icone="exclamation-triangle">Mais de {{ limite }} dias sem atualizar</cw-selo>
        }
      } @else {
        <p class="valor vazio">Sem cotação</p>
        <p class="quando">Registre o valor do grama de {{ puro() }} para orçar.</p>
      }

      @if (editando()) {
        <form class="atualizar" [formGroup]="form" (ngSubmit)="salvar()" novalidate>
          <label class="form-label" [for]="'valor-' + id()">R$ por grama de {{ puro() }}</label>
          <div class="linha">
            <input
              [id]="'valor-' + id()"
              type="number"
              step="0.01"
              min="0.01"
              inputmode="decimal"
              class="form-control"
              formControlName="valor"
              [class.is-invalid]="valor.touched && valor.invalid"
            />
            <button type="submit" class="btn btn-primary" [disabled]="salvando()">{{ salvando() ? 'Salvando...' : 'Salvar' }}</button>
            <button type="button" class="btn btn-sutil" (click)="editando.set(false)" [disabled]="salvando()">Cancelar</button>
          </div>
          @if (valor.touched && valor.invalid) {
            <p class="erro" role="alert">{{ valor.errors?.['servidor'] ?? 'Informe um valor maior que zero.' }}</p>
          }
        </form>
      } @else {
        <div class="acoes">
          @if (podeEditar()) {
            <button type="button" class="btn btn-secundario" (click)="editar()"><i class="bi bi-arrow-repeat" aria-hidden="true"></i> Atualizar</button>
          }
          <button type="button" class="btn btn-sutil" (click)="historicoAberto.set(!historicoAberto())" [attr.aria-expanded]="historicoAberto()">
            <i class="bi bi-clock-history" aria-hidden="true"></i> Histórico
          </button>
        </div>
      }

      @if (historicoAberto()) {
        <div class="historico">
          @if (historico.error()) {
            <p class="quando">Não foi possível carregar o histórico.</p>
          } @else if (valorDe(historico); as pagina) {
            @if (pagina.nodes.length === 0) {
              <p class="quando">Nenhuma cotação registrada.</p>
            } @else {
              <ol>
                @for (item of pagina.nodes; track item.registradaEm) {
                  <li>
                    <span class="data">{{ item.registradaEm | date: 'dd/MM/yyyy HH:mm' }}</span>
                    <span class="autor">{{ item.registradaPor ?? '—' }}</span>
                    <span class="valor-item">{{ moeda(item.valor) }}/g</span>
                  </li>
                }
              </ol>
              <cw-paginacao
                [total]="pagina.totalCount"
                [pagina]="paginacao.numero()"
                singular="cotação"
                plural="cotações"
                [temAnterior]="paginacao.temAnterior()"
                [temProxima]="pagina.pageInfo.hasNextPage"
                (anterior)="paginacao.anterior()"
                (proxima)="paginacao.proxima(pagina.pageInfo.endCursor)"
              />
            }
          } @else {
            <p class="quando" aria-busy="true">Carregando...</p>
          }
        </div>
      }
    </article>
  `,
  styles: `
    .cartao {
      height: 100%;
      padding: 1.25rem;
      border: 1px solid var(--cw-border);
      border-radius: var(--cw-radius-lg);
      background: var(--cw-surface);
    }
    h3 {
      margin: 0;
      font-size: 1rem;
      font-weight: 600;
    }
    .puro {
      margin-left: 0.25rem;
      font-weight: 400;
      color: var(--cw-ink-muted);
      font-size: 0.875rem;
    }
    .valor {
      margin: 0.5rem 0 0;
      font-family: 'Fraunces Variable', serif;
      font-size: 1.75rem;
      font-variant-numeric: tabular-nums;
    }
    .valor.vazio {
      color: var(--cw-ink-faint);
    }
    .unidade {
      font-size: 1rem;
      color: var(--cw-ink-muted);
    }
    .quando {
      margin: 0.25rem 0 0.75rem;
      color: var(--cw-ink-muted);
      font-size: 0.875rem;
    }
    .acoes,
    .linha {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-top: 0.75rem;
    }
    .linha .form-control {
      flex: 1 1 8rem;
    }
    .btn {
      min-height: var(--cw-alvo);
    }
    .atualizar {
      margin-top: 0.75rem;
    }
    .erro {
      margin: 0.375rem 0 0;
      color: var(--cw-danger-ink);
      font-size: 0.8125rem;
      font-weight: 500;
    }
    .historico {
      margin-top: 1rem;
      padding-top: 0.75rem;
      border-top: 1px solid var(--cw-border);
    }
    ol {
      list-style: none;
      margin: 0 0 0.5rem;
      padding: 0;
    }
    li {
      display: flex;
      gap: 0.75rem;
      padding: 0.375rem 0;
      font-size: 0.875rem;
      font-variant-numeric: tabular-nums;
    }
    .autor {
      flex: 1 1 auto;
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      color: var(--cw-ink-muted);
    }
    .valor-item {
      font-weight: 600;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CotacaoMetalComponent {
  protected readonly valorDe = valorDe;
  private readonly precosService = inject(PrecosService);
  private readonly toast = inject(ToastService);

  readonly cotacao = input.required<CotacaoVigenteDto>();
  readonly podeEditar = input(false);
  /** O valor foi registrado: a página relê os preços. */
  readonly atualizada = output();

  protected readonly moeda = moeda;
  protected readonly haQuanto = haQuanto;
  protected readonly limite = DIAS_PARA_DESATUALIZAR;
  protected readonly paginacao = new PaginacaoCursor();

  protected readonly id = computed(() => this.cotacao().metal.toLowerCase());
  protected readonly rotulo = computed(() => rotuloMetal(this.cotacao().metal));
  protected readonly puro = computed(() => metalPuro(this.cotacao().metal));
  protected readonly dias = computed(() => {
    const atual = this.cotacao().atual;
    return atual ? diasDesde(atual.registradaEm) : null;
  });
  protected readonly desatualizada = computed(() => (this.dias() ?? 0) > DIAS_PARA_DESATUALIZAR);

  protected readonly editando = signal(false);
  protected readonly salvando = signal(false);
  protected readonly historicoAberto = signal(false);
  /** Grupo de um campo só: o 400 do servidor ("valor") cai no controle pelo nome. */
  protected readonly form = new FormGroup({ valor: new FormControl<number | null>(null, [Validators.required, Validators.min(0.01)]) });
  protected readonly valor = this.form.controls.valor;

  /** Só busca o histórico quando aberto; relê quando a cotação vigente muda. */
  protected readonly historico = rxResource({
    params: () =>
      this.historicoAberto()
        ? { metal: this.cotacao().metal, vigente: this.cotacao().atual?.registradaEm, apos: this.paginacao.apos(), primeiros: POR_PAGINA }
        : undefined,
    stream: ({ params }) => this.precosService.getHistoricoDeCotacoes(params),
  });

  protected editar(): void {
    this.valor.reset(this.cotacao().atual?.valor ?? null);
    this.editando.set(true);
  }

  protected salvar(): void {
    if (this.valor.invalid || this.valor.value === null) {
      this.valor.markAsTouched();
      return;
    }
    this.salvando.set(true);
    this.precosService
      .postCotacao({ metal: this.cotacao().metal, valor: this.valor.value })
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: () => {
          this.toast.sucesso(`Cotação de ${this.rotulo().toLowerCase()} atualizada. A anterior fica no histórico.`);
          this.editando.set(false);
          this.paginacao.reiniciar();
          this.atualizada.emit();
        },
        // O interceptor global já avisa; aqui o erro de validação vai para o campo.
        error: (erro: unknown) => aplicarErrosDoServidor(this.form, erro),
      });
  }
}

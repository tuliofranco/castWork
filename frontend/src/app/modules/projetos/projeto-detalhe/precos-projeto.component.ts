import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import type { Metal, ParametrosDePrecoDto } from '@data/precificacao/precos/dto/precos.dto';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import type { PrecosDoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { ToastService } from '@shared/services/toast.service';
import { CampoComponent } from '@shared/ui/campo.component';
import { finalize } from 'rxjs';

const METAIS: readonly { readonly valor: Metal; readonly rotulo: string; readonly controle: 'ouro' | 'prata' }[] = [
  { valor: 'OURO', rotulo: 'Ouro', controle: 'ouro' },
  { valor: 'PRATA', rotulo: 'Prata', controle: 'prata' },
];

const PERCENTUAL = [Validators.required, Validators.min(0), Validators.max(50)];
const MENSAGENS_PERCENTUAL = { required: 'Informe o percentual.', min: 'Entre 0% e 50%.', max: 'Entre 0% e 50%.' };
const MENSAGENS_COTACAO = { min: 'Deve ser maior que zero.' };

/** 640 → "R$ 640,00". */
function moeda(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

/** 1.8 → "1,8". */
function numero(valor: number): string {
  return valor.toLocaleString('pt-BR', { maximumFractionDigits: 2 });
}

/** Cada parâmetro como aparece no resumo ("Perda 8 %"). */
const PARAMETROS: readonly { readonly chave: keyof ParametrosDePrecoDto; readonly rotulo: (v: number) => string }[] = [
  { chave: 'perdaPercentual', rotulo: (v) => `Perda ${numero(v)} %` },
  { chave: 'valorHora', rotulo: (v) => (v === 0 ? 'Hora a definir' : `Hora ${moeda(v)}`) },
  { chave: 'markup', rotulo: (v) => `Markup ${numero(v)}×` },
  { chave: 'taxasPercentual', rotulo: (v) => `Taxas ${numero(v)} %` },
  { chave: 'arredondarPara', rotulo: (v) => (v === 0 ? 'Sem arredondamento' : `Arredonda para ${moeda(v)}`) },
];

/**
 * Preços deste projeto: a cópia dos preços do ateliê feita na criação, editável só aqui. Mostra a diferença para os
 * valores de hoje do ateliê e oferece "atualizar para os valores atuais" (com confirmação). Só entra na página para
 * quem tem `precificacao.ler`; editar e atualizar pedem `precificacao.editar`.
 */
@Component({
  selector: 'cw-precos-projeto',
  imports: [DatePipe, ReactiveFormsModule, CampoComponent],
  template: `
    <section class="secao" aria-labelledby="titulo-precos-projeto">
      <div class="cabecalho-secao">
        <h2 id="titulo-precos-projeto">Preços deste projeto</h2>
        @if (podeEditar() && precos.value() && !editando()) {
          <button type="button" class="btn btn-sutil" [disabled]="salvando()" (click)="abrirEdicao()"><i class="bi bi-pencil" aria-hidden="true"></i> Editar</button>
        }
      </div>

      @if (precos.error()) {
        <p class="falha" role="alert">Não foi possível carregar os preços do projeto.</p>
      } @else if (precos.value(); as p) {
        @if (editando()) {
          <form class="cw-formulario" [formGroup]="form" (ngSubmit)="salvar()" novalidate>
            <div class="grade">
              @for (m of metais; track m.valor) {
                <cw-campo class="c3" [rotulo]="m.rotulo + ' (R$/g do metal puro)'" [para]="'cotacao-' + m.controle" [controle]="form.controls[m.controle]" [mensagens]="mensagensCotacao" [opcional]="true">
                  <input [id]="'cotacao-' + m.controle" type="number" step="0.01" min="0.01" inputmode="decimal" class="form-control" [formControlName]="m.controle" />
                </cw-campo>
              }
              <cw-campo class="c2" rotulo="Perda (%)" para="precos-perda" [controle]="form.controls.perdaPercentual" [mensagens]="mensagensPercentual">
                <input id="precos-perda" type="number" step="0.01" min="0" max="50" inputmode="decimal" class="form-control" formControlName="perdaPercentual" />
              </cw-campo>
              <cw-campo class="c2" rotulo="Valor da hora (R$)" para="precos-hora" [controle]="form.controls.valorHora" [mensagens]="{ required: 'Informe o valor da hora.', min: 'Deve ser maior que zero.' }">
                <input id="precos-hora" type="number" step="0.01" min="0.01" inputmode="decimal" class="form-control" formControlName="valorHora" />
              </cw-campo>
              <cw-campo class="c2" rotulo="Markup (×)" para="precos-markup" [controle]="form.controls.markup" [mensagens]="{ required: 'Informe o markup.', min: 'Deve ser maior que zero.', max: 'Até 10×.' }">
                <input id="precos-markup" type="number" step="0.01" min="0.01" max="10" inputmode="decimal" class="form-control" formControlName="markup" />
              </cw-campo>
              <cw-campo class="c3" rotulo="Taxas (%)" para="precos-taxas" [controle]="form.controls.taxasPercentual" [mensagens]="mensagensPercentual">
                <input id="precos-taxas" type="number" step="0.01" min="0" max="50" inputmode="decimal" class="form-control" formControlName="taxasPercentual" />
              </cw-campo>
              <cw-campo class="c3" rotulo="Arredondar para (R$)" para="precos-arredondar" [controle]="form.controls.arredondarPara" [mensagens]="{ required: 'Informe o múltiplo.', min: 'Entre 0 e 1.000.', max: 'Entre 0 e 1.000.' }">
                <input id="precos-arredondar" type="number" step="1" min="0" max="1000" inputmode="decimal" class="form-control" formControlName="arredondarPara" />
              </cw-campo>
            </div>
            <p class="dica">Vale só para este projeto: o ateliê e os outros projetos não mudam.</p>
            <div class="acoes">
              <button type="button" class="btn btn-secundario" [disabled]="salvando()" (click)="editando.set(false)">Cancelar</button>
              <button type="submit" class="btn btn-primary" [disabled]="salvando()">{{ salvando() ? 'Salvando...' : 'Salvar preços' }}</button>
            </div>
          </form>
        } @else {
          <p class="resumo">{{ resumo() }}</p>
          <p class="origem">
            <span>herdado do ateliê em {{ p.herdadosEm | date: 'dd/MM/yyyy' }}</span>
            @if (p.alteradosEm) {
              <span>· editado neste projeto em {{ p.alteradosEm | date: 'dd/MM/yyyy' }}</span>
            }
            @if (podeEditar() && !confirmandoAtualizar()) {
              <span>·</span>
              <button type="button" class="btn btn-sutil atualizar" [disabled]="salvando()" (click)="confirmandoAtualizar.set(true)">Atualizar para os valores atuais</button>
            }
          </p>
          @if (diferencas().length > 0) {
            <p class="diferenca"><i class="bi bi-arrow-left-right" aria-hidden="true"></i> Ateliê hoje: {{ diferencas().join(' · ') }}</p>
          }
          @if (confirmandoAtualizar()) {
            <div class="confirmar" role="alertdialog" aria-labelledby="texto-atualizar-precos">
              <span id="texto-atualizar-precos">Substituir as cotações e os parâmetros deste projeto pelos valores atuais do ateliê? O que foi editado aqui se perde.</span>
              <span class="botoes">
                <button type="button" class="btn btn-sutil" (click)="confirmandoAtualizar.set(false)">Cancelar</button>
                <button type="button" class="btn btn-primary" [disabled]="salvando()" (click)="atualizar()">Substituir</button>
              </span>
            </div>
          }
        }
      } @else {
        <p class="carregando" aria-busy="true">Carregando os preços...</p>
      }
    </section>
  `,
  styles: `
    .secao {
      margin-bottom: 2.25rem;
    }
    .cabecalho-secao {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 0.75rem;
    }
    h2 {
      margin: 0 auto 0 0;
      font-size: 1.0625rem;
      font-weight: 600;
    }
    .resumo {
      margin: 0 0 0.25rem;
      font-variant-numeric: tabular-nums;
    }
    .origem {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.375rem;
      margin: 0;
      color: var(--cw-ink-muted);
      font-size: 0.875rem;
    }
    .atualizar {
      padding: 0.25rem 0.5rem;
      font-size: 0.875rem;
    }
    .diferenca {
      display: flex;
      gap: 0.5rem;
      align-items: center;
      margin: 0.75rem 0 0;
      padding: 0.5rem 0.875rem;
      border-radius: var(--cw-radius);
      background: var(--cw-warning-soft);
      color: var(--cw-warning-ink);
      font-size: 0.875rem;
    }
    .confirmar {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.75rem;
      margin-top: 0.75rem;
      padding: 0.75rem 1rem;
      border: 1px solid var(--cw-border);
      border-radius: var(--cw-radius-lg);
    }
    .confirmar .botoes {
      display: flex;
      gap: 0.5rem;
      margin-left: auto;
    }
    .dica,
    .carregando,
    .falha {
      margin: 1rem 0 0;
      color: var(--cw-ink-muted);
      font-size: 0.875rem;
    }
    .falha {
      color: var(--cw-danger-ink);
    }
    .acoes {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-top: 1rem;
    }
    .btn {
      min-height: var(--cw-alvo);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrecosProjetoComponent {
  private readonly projetosService = inject(ProjetosService);
  private readonly precosService = inject(PrecosService);
  private readonly toast = inject(ToastService);

  readonly projetoId = input.required<number>();
  /** `precificacao.editar`: editar e atualizar para os valores atuais. */
  readonly podeEditar = input(false);

  protected readonly metais = METAIS;
  protected readonly mensagensPercentual = MENSAGENS_PERCENTUAL;
  protected readonly mensagensCotacao = MENSAGENS_COTACAO;
  protected readonly editando = signal(false);
  protected readonly confirmandoAtualizar = signal(false);
  protected readonly salvando = signal(false);

  protected readonly precos = rxResource({
    params: () => this.projetoId(),
    stream: ({ params }) => this.projetosService.getPrecosDoProjeto(params),
  });

  /** Valores de hoje do ateliê, para mostrar a diferença. */
  protected readonly atelie = rxResource({ stream: () => this.precosService.getPrecos() });

  protected readonly form = inject(FormBuilder).group({
    ouro: [null as number | null, [Validators.min(0.01)]],
    prata: [null as number | null, [Validators.min(0.01)]],
    perdaPercentual: [null as number | null, PERCENTUAL],
    valorHora: [null as number | null, [Validators.required, Validators.min(0.01)]],
    markup: [null as number | null, [Validators.required, Validators.min(0.01), Validators.max(10)]],
    taxasPercentual: [null as number | null, PERCENTUAL],
    arredondarPara: [null as number | null, [Validators.required, Validators.min(0), Validators.max(1000)]],
  });

  /** "Ouro R$ 640,00/g · Prata R$ 6,80/g · Perda 8 % · Hora R$ 90,00 · Markup 1,8×". */
  protected readonly resumo = computed(() => {
    const p = this.precos.value();
    if (!p) return '';
    const cotacoes = METAIS.flatMap((m) => {
      const cotacao = p.cotacoes.find((c) => c.metal === m.valor);
      return cotacao ? [`${m.rotulo} ${moeda(cotacao.valor)}/g`] : [];
    });
    const parametros = PARAMETROS.slice(0, 3).map((x) => x.rotulo(p.parametros[x.chave]));
    return [...(cotacoes.length > 0 ? cotacoes : ['Sem cotação de metal']), ...parametros].join(' · ');
  });

  /** O que o ateliê tem hoje de diferente do projeto (editado aqui ou mudado lá depois da herança). */
  protected readonly diferencas = computed(() => {
    const p = this.precos.value();
    const a = this.atelie.value();
    if (!p || !a) return [];
    const cotacoes = METAIS.flatMap((m) => {
      const hoje = a.cotacoes.find((c) => c.metal === m.valor)?.atual?.valor ?? null;
      const aqui = p.cotacoes.find((c) => c.metal === m.valor)?.valor ?? null;
      return hoje !== null && hoje !== aqui ? [`${m.rotulo} ${moeda(hoje)}/g`] : [];
    });
    const parametros = PARAMETROS.filter((x) => a.parametros[x.chave] !== p.parametros[x.chave]).map((x) => x.rotulo(a.parametros[x.chave]));
    return [...cotacoes, ...parametros];
  });

  protected abrirEdicao(): void {
    const p = this.precos.value();
    if (!p) return;
    this.preencher(p);
    this.confirmandoAtualizar.set(false);
    this.editando.set(true);
  }

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const cotacoes = METAIS.flatMap((m) => {
      const valor = v[m.controle];
      return valor === null ? [] : [{ metal: m.valor, valor }];
    });
    this.salvando.set(true);
    this.projetosService
      .putPrecos(this.projetoId(), {
        cotacoes,
        perdaPercentual: v.perdaPercentual ?? 0,
        valorHora: v.valorHora ?? 0,
        markup: v.markup ?? 0,
        taxasPercentual: v.taxasPercentual ?? 0,
        arredondarPara: v.arredondarPara ?? 0,
      })
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: () => {
          this.toast.sucesso('Preços do projeto salvos. O ateliê não mudou.');
          this.editando.set(false);
          this.precos.reload();
        },
        error: (erro: unknown) => aplicarErrosDoServidor(this.form, erro),
      });
  }

  protected atualizar(): void {
    this.salvando.set(true);
    this.projetosService
      .postAtualizarPrecos(this.projetoId())
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe(() => {
        this.toast.sucesso('Preços atualizados para os valores atuais do ateliê.');
        this.confirmandoAtualizar.set(false);
        this.precos.reload();
        this.atelie.reload();
      });
  }

  private preencher(p: PrecosDoProjetoDto): void {
    const cotacao = (metal: Metal) => p.cotacoes.find((c) => c.metal === metal)?.valor ?? null;
    // Valor da hora zero é "a definir" (herdado assim do ateliê): o campo começa vazio e pede o valor.
    this.form.reset({ ...p.parametros, valorHora: p.parametros.valorHora === 0 ? null : p.parametros.valorHora, ouro: cotacao('OURO'), prata: cotacao('PRATA') });
  }
}

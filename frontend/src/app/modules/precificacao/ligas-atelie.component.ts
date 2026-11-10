import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import type { LigaDto, Metal } from '@data/precificacao/precos/dto/precos.dto';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { formatarTeor } from '@shared/pecas/tipo-peca';
import { ToastService } from '@shared/services/toast.service';
import { CampoComponent } from '@shared/ui/campo.component';
import { finalize, type Observable } from 'rxjs';

import { METAIS, numero, rotuloMetal } from './formatos';

/** `null`: nenhuma em edição; `'nova'`: cadastrando; número: Id da liga em edição. */
type Edicao = number | 'nova' | null;

/**
 * Ligas do ateliê: metal, teor e densidade. As pré-cadastradas são editáveis; liga não se exclui, desativa
 * (some da tela e dos orçamentos novos).
 */
@Component({
  selector: 'cw-ligas-atelie',
  imports: [NgTemplateOutlet, ReactiveFormsModule, CampoComponent],
  template: `
    <ul class="cw-lista">
      @for (liga of ligas(); track liga.id) {
        <li>
          @if (editando() === liga.id) {
            <ng-container *ngTemplateOutlet="formulario" />
          } @else if (desativando() === liga.id) {
            <div class="cw-linha confirmar" role="group" [attr.aria-label]="'Desativar ' + liga.nome">
              <span class="principal">
                <span class="nome">Desativar {{ liga.nome }}?</span>
                <span class="meta">Ela some desta tela e dos orçamentos novos.</span>
              </span>
              <button type="button" class="btn btn-sutil" (click)="desativando.set(null)" [disabled]="salvando()">Cancelar</button>
              <button type="button" class="btn btn-secundario" (click)="desativar(liga)" [disabled]="salvando()">Desativar</button>
            </div>
          } @else {
            <div class="cw-linha">
              <span class="cw-icone-linha"><i class="bi bi-droplet-half" aria-hidden="true"></i></span>
              <span class="principal">
                <span class="nome">{{ liga.nome }}</span>
                <span class="meta">{{ rotuloMetal(liga.metal) }} · teor {{ formatarTeor(liga.teor) }} · {{ numero(liga.densidade) }} g/cm³</span>
              </span>
              @if (podeEditar()) {
                <button type="button" class="btn btn-sutil" [attr.aria-label]="'Editar ' + liga.nome" (click)="editar(liga)" [disabled]="editando() !== null">
                  <i class="bi bi-pencil" aria-hidden="true"></i> Editar
                </button>
                <button type="button" class="btn btn-perigo-sutil" [attr.aria-label]="'Desativar ' + liga.nome" (click)="desativando.set(liga.id)" [disabled]="editando() !== null">
                  <i class="bi bi-pause-circle" aria-hidden="true"></i> Desativar
                </button>
              }
            </div>
          }
        </li>
      } @empty {
        <li><p class="cw-linha meta">Nenhuma liga ativa.</p></li>
      }
      @if (editando() === 'nova') {
        <li><ng-container *ngTemplateOutlet="formulario" /></li>
      }
    </ul>

    @if (podeEditar() && editando() === null) {
      <button type="button" class="btn btn-secundario nova" (click)="nova()"><i class="bi bi-plus-lg" aria-hidden="true"></i> Nova liga</button>
    }

    <ng-template #formulario>
      <form class="edicao" [formGroup]="form" (ngSubmit)="salvar()" novalidate>
        <div class="grade">
          <cw-campo class="g-nome" rotulo="Nome" para="liga-nome" [controle]="form.controls.nome" [mensagens]="{ required: 'Informe o nome.' }">
            <input id="liga-nome" class="form-control" formControlName="nome" maxlength="60" placeholder="Ex.: Ouro 18k amarelo" autocomplete="off" />
          </cw-campo>
          <cw-campo rotulo="Metal" para="liga-metal" [controle]="form.controls.metal">
            <select id="liga-metal" class="form-select" formControlName="metal">
              @for (metal of metais; track metal.valor) {
                <option [value]="metal.valor">{{ metal.rotulo }}</option>
              }
            </select>
          </cw-campo>
          <cw-campo rotulo="Teor" para="liga-teor" [controle]="form.controls.teor" dica="Milésimos (750) ou quilates (18k)." [mensagens]="{ required: 'Informe o teor.' }">
            <input id="liga-teor" class="form-control" formControlName="teor" autocomplete="off" />
          </cw-campo>
          <cw-campo rotulo="Densidade (g/cm³)" para="liga-densidade" [controle]="form.controls.densidade" [mensagens]="{ required: 'Informe a densidade.', min: 'Deve ser maior que zero.', max: 'Até 25 g/cm³.' }">
            <input id="liga-densidade" type="number" step="0.001" min="0.001" max="25" inputmode="decimal" class="form-control" formControlName="densidade" />
          </cw-campo>
        </div>
        <div class="acoes">
          <button type="button" class="btn btn-sutil" (click)="cancelar()" [disabled]="salvando()">Cancelar</button>
          <button type="submit" class="btn btn-primary" [disabled]="salvando()">{{ salvando() ? 'Salvando...' : 'Salvar liga' }}</button>
        </div>
      </form>
    </ng-template>
  `,
  styles: `
    .cw-linha .btn,
    .acoes .btn,
    .nova {
      min-height: var(--cw-alvo);
    }
    .confirmar {
      background: var(--cw-danger-soft);
    }
    .nova {
      margin-top: 1rem;
    }
    .edicao {
      padding: 1rem 1.25rem;
      background: var(--cw-surface-2);
    }
    .grade {
      display: grid;
      gap: 1rem;
      @media (min-width: 768px) {
        grid-template-columns: 2fr 1fr 1fr 1fr;
      }
    }
    .acoes {
      display: flex;
      justify-content: flex-end;
      gap: 0.5rem;
      margin-top: 1rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LigasAtelieComponent {
  private readonly precosService = inject(PrecosService);
  private readonly toast = inject(ToastService);

  readonly ligas = input.required<readonly LigaDto[]>();
  readonly podeEditar = input(false);
  /** Alguma liga foi criada, alterada ou desativada: a página relê os preços. */
  readonly alteradas = output();

  protected readonly metais = METAIS;
  protected readonly rotuloMetal = rotuloMetal;
  protected readonly formatarTeor = formatarTeor;
  protected readonly numero = numero;

  protected readonly editando = signal<Edicao>(null);
  protected readonly desativando = signal<number | null>(null);
  protected readonly salvando = signal(false);

  // Teor e nome repetido são conferidos no servidor; o erro volta para o campo.
  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(60)]],
    metal: ['OURO' as Metal, Validators.required],
    teor: ['', Validators.required],
    densidade: [null as number | null, [Validators.required, Validators.min(0.001), Validators.max(25)]],
  });

  protected nova(): void {
    this.form.reset({ nome: '', metal: 'OURO', teor: '', densidade: null });
    this.desativando.set(null);
    this.editando.set('nova');
  }

  protected editar(liga: LigaDto): void {
    this.form.reset({ nome: liga.nome, metal: liga.metal, teor: String(liga.teor), densidade: liga.densidade });
    this.desativando.set(null);
    this.editando.set(liga.id);
  }

  protected cancelar(): void {
    this.editando.set(null);
  }

  protected salvar(): void {
    const edicao = this.editando();
    if (this.form.invalid || edicao === null) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const dto = { nome: v.nome, metal: v.metal, teor: v.teor, densidade: v.densidade ?? 0 };
    const envio: Observable<unknown> = edicao === 'nova' ? this.precosService.postLiga(dto) : this.precosService.putLiga(edicao, dto);
    this.executar(envio, edicao === 'nova' ? 'Liga cadastrada.' : 'Liga alterada.', () => this.editando.set(null));
  }

  protected desativar(liga: LigaDto): void {
    this.executar(this.precosService.desativarLiga(liga.id), `${liga.nome} desativada.`, () => this.desativando.set(null));
  }

  private executar(envio: Observable<unknown>, mensagem: string, aoConcluir: () => void): void {
    this.salvando.set(true);
    envio.pipe(finalize(() => this.salvando.set(false))).subscribe({
      next: () => {
        this.toast.sucesso(mensagem);
        aoConcluir();
        this.alteradas.emit();
      },
      error: (erro: unknown) => aplicarErrosDoServidor(this.form, erro),
    });
  }
}

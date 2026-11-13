import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import type { ParametrosDePrecoDto } from '@data/precificacao/precos/dto/precos.dto';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { ToastService } from '@shared/services/toast.service';
import { CampoComponent } from '@shared/ui/campo.component';
import { finalize } from 'rxjs';

const PERCENTUAL = [Validators.required, Validators.min(0), Validators.max(50)];
const MENSAGENS_PERCENTUAL = { required: 'Informe o percentual.', min: 'Entre 0% e 50%.', max: 'Entre 0% e 50%.' };

/**
 * Parâmetros que entram em todo orçamento, com a explicação de cada um. Sem a regra de edição, só leitura.
 */
@Component({
  selector: 'cw-parametros-preco',
  imports: [ReactiveFormsModule, CampoComponent],
  template: `
    @if (parametros().valorHora === 0) {
      <p class="aviso" role="status"><i class="bi bi-exclamation-triangle" aria-hidden="true"></i> Defina o valor da hora de bancada antes de orçar.</p>
    }
    <form class="cw-formulario" [formGroup]="form" (ngSubmit)="salvar()" novalidate>
      <div class="grade">
        <cw-campo class="c3" rotulo="Perda de fundição e acabamento (%)" para="perdaPercentual" [controle]="form.controls.perdaPercentual" [mensagens]="mensagensPercentual"
          dica="Metal que se perde entre o 3D e a peça pronta (canais, botão, polimento).">
          <input id="perdaPercentual" type="number" step="0.01" min="0" max="50" inputmode="decimal" class="form-control" formControlName="perdaPercentual" />
        </cw-campo>
        <cw-campo class="c3" rotulo="Valor da hora (R$)" para="valorHora" [controle]="form.controls.valorHora"
          [mensagens]="{ required: 'Informe o valor da hora.', min: 'Deve ser maior que zero.' }" dica="Hora de bancada, multiplicada pelas horas de cada versão.">
          <input id="valorHora" type="number" step="0.01" min="0.01" inputmode="decimal" class="form-control" formControlName="valorHora" />
        </cw-campo>
        <cw-campo class="c2" rotulo="Markup (×)" para="markup" [controle]="form.controls.markup"
          [mensagens]="{ required: 'Informe o markup.', min: 'Deve ser maior que zero.', max: 'Até 10×.' }" dica="Fator sobre o custo: 1,8 vende por 1,8 vez o custo.">
          <input id="markup" type="number" step="0.01" min="0.01" max="10" inputmode="decimal" class="form-control" formControlName="markup" />
        </cw-campo>
        <cw-campo class="c2" rotulo="Taxas (%)" para="taxasPercentual" [controle]="form.controls.taxasPercentual" [mensagens]="mensagensPercentual"
          dica="Cartão e impostos, somados sobre o preço.">
          <input id="taxasPercentual" type="number" step="0.01" min="0" max="50" inputmode="decimal" class="form-control" formControlName="taxasPercentual" />
        </cw-campo>
        <cw-campo class="c2" rotulo="Arredondar para (R$)" para="arredondarPara" [controle]="form.controls.arredondarPara"
          [mensagens]="{ required: 'Informe o múltiplo.', min: 'Entre 0 e 1.000.', max: 'Entre 0 e 1.000.' }" dica="O preço sobe até o múltiplo deste valor. Zero não arredonda.">
          <input id="arredondarPara" type="number" step="1" min="0" max="1000" inputmode="decimal" class="form-control" formControlName="arredondarPara" />
        </cw-campo>
      </div>
      @if (podeEditar()) {
        <div class="acoes">
          <button type="button" class="btn btn-secundario" (click)="desfazer()" [disabled]="salvando() || form.pristine">Desfazer</button>
          <button type="submit" class="btn btn-primary" [disabled]="salvando()">{{ salvando() ? 'Salvando...' : 'Salvar parâmetros' }}</button>
        </div>
      }
    </form>
  `,
  styles: `
    .aviso {
      display: flex;
      gap: 0.5rem;
      align-items: center;
      margin: 0 0 1rem;
      padding: 0.75rem 1rem;
      border-radius: var(--cw-radius);
      background: var(--cw-warning-soft);
      color: var(--cw-warning-ink);
    }
    .acoes {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-top: 1.5rem;
    }
    .acoes .btn {
      min-height: var(--cw-alvo);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ParametrosPrecoComponent {
  private readonly precosService = inject(PrecosService);
  private readonly toast = inject(ToastService);

  readonly parametros = input.required<ParametrosDePrecoDto>();
  readonly podeEditar = input(false);
  /** Salvos: a página relê os preços. */
  readonly salvos = output();

  protected readonly mensagensPercentual = MENSAGENS_PERCENTUAL;
  protected readonly salvando = signal(false);

  protected readonly form = inject(FormBuilder).group({
    perdaPercentual: [null as number | null, PERCENTUAL],
    valorHora: [null as number | null, [Validators.required, Validators.min(0.01)]],
    markup: [null as number | null, [Validators.required, Validators.min(0.01), Validators.max(10)]],
    taxasPercentual: [null as number | null, PERCENTUAL],
    arredondarPara: [null as number | null, [Validators.required, Validators.min(0), Validators.max(1000)]],
  });

  constructor() {
    // Acompanha os valores salvos quando a página relê, menos enquanto há edição não salva aqui: outro bloco da
    // página salvar não pode apagar o que está sendo digitado. Sem a regra, o formulário é só leitura.
    effect(() => {
      const parametros = this.parametros();
      if (!untracked(() => this.form.dirty)) this.preencher(parametros);
      if (this.podeEditar()) this.form.enable();
      else this.form.disable();
    });
  }

  protected desfazer(): void {
    this.preencher(this.parametros());
  }

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.salvando.set(true);
    this.precosService
      .putParametros({
        perdaPercentual: v.perdaPercentual ?? 0,
        valorHora: v.valorHora ?? 0,
        markup: v.markup ?? 0,
        taxasPercentual: v.taxasPercentual ?? 0,
        arredondarPara: v.arredondarPara ?? 0,
      })
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: () => {
          this.form.markAsPristine();
          this.toast.sucesso('Parâmetros salvos. Valem para os próximos orçamentos.');
          this.salvos.emit();
        },
        error: (erro: unknown) => aplicarErrosDoServidor(this.form, erro),
      });
  }

  private preencher(p: ParametrosDePrecoDto): void {
    // Valor da hora zero é "a definir": o campo começa vazio e pede o valor.
    this.form.reset({ ...p, valorHora: p.valorHora === 0 ? null : p.valorHora });
  }
}

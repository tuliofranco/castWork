import { moeda, numero } from '@shared/formatos/numeros';
import { valorDe } from '@shared/recursos/valor-de';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import type { Metal } from '@data/precificacao/precos/dto/precos.dto';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import type {
  CalculoDoPrecoDto,
  CustoDoMetalDto,
  PendenciaDoPrecoDto,
  TipoDeAviso,
  TipoMaoDeObra,
} from '@data/projetos/projetos/dto/preco-da-versao.dto';
import type { VersaoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { SeloComponent } from '@shared/ui/selo.component';
import { type Observable, finalize } from 'rxjs';

type Edicao = 'metais' | 'maoDeObra' | 'servicos' | 'markup' | null;

const NOME_DO_METAL: Readonly<Record<Metal, string>> = { OURO: 'ouro', PRATA: 'prata' };

const ICONE_DO_AVISO: Readonly<Record<TipoDeAviso, string>> = {
  COTACAO_ANTIGA: 'bi-clock-history',
  CORPOS_SOBREPOSTOS: 'bi-intersect',
  CORPO_ABERTO: 'bi-exclamation-circle',
};

/** 5 → "5,00 g"; 5.125 → "5,125 g". */
function gramas(valor: number): string {
  return `${valor.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 3 })} g`;
}

/** 4 → "+4%"; -2.5 → "-2,5%". */
function diferenca(valor: number): string {
  return `${valor > 0 ? '+' : ''}${valor.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`;
}

/** "Ouro 18k amarelo: 5,00 g + perda 8% = 5,40 g × R$ 480,00/g (R$ 640,00 × 0,750)". */
function linhaDoMetal(m: CustoDoMetalDto, perda: number): string {
  const peso = `${m.liga}: ${gramas(m.pesoEstimado)} + perda ${numero(perda)}% = ${gramas(m.pesoAFundir)}`;
  if (m.valorPorGrama === null || m.cotacaoDoPuro === null)
    return `${peso}, sem cotação de ${NOME_DO_METAL[m.metal]}`;
  const pureza = (m.teor / 1000).toLocaleString('pt-BR', { minimumFractionDigits: 3 });
  return `${peso} × ${moeda(m.valorPorGrama)}/g (${moeda(m.cotacaoDoPuro)} × ${pureza})`;
}

/**
 * Card "Preço" da versão: o cálculo linha a linha (metais, composição, mão de obra e serviços), o preço em destaque, o
 * selo Completo/Incompleto com as pendências e um atalho para cada uma, e a edição do orçamento. O peso do metal vem do
 * modelo 3D quando a versão tem um ("Peso da peça (3D)"); sem 3D, é o estimado manualmente. Com 3D, o peso manual fica
 * guardado e a tela diz que não é usado. O cálculo é refeito a cada leitura. Sem `precificacao.ler`, só o preço e o
 * status; editar pede `precificacao.editar` e versão em desenvolvimento (o peso real vale em qualquer status).
 */
@Component({
  selector: 'cw-preco-versao',
  imports: [ReactiveFormsModule, RouterLink, SeloComponent],
  templateUrl: './preco-versao.component.html',
  styleUrl: './preco-versao.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrecoVersaoComponent {
  protected readonly valorDe = valorDe;
  private readonly projetosService = inject(ProjetosService);
  private readonly precosService = inject(PrecosService);
  private readonly toast = inject(ToastService);

  readonly projetoId = input.required<number>();
  /** A versão como a página leu: quando a composição muda, a página relê e o preço é recalculado. */
  readonly versao = input.required<VersaoProjetoDto>();
  /** `precificacao.ler`: o cálculo detalhado. */
  readonly podeVerCalculo = input(false);
  /** `precificacao.editar`: metais, mão de obra, serviços, markup e peso real. */
  readonly podeEditar = input(false);

  protected readonly moeda = moeda;
  protected readonly gramas = gramas;
  protected readonly diferenca = diferenca;
  protected readonly numero = numero;
  protected readonly salvando = signal(false);
  protected readonly editando = signal<Edicao>(null);
  /** Liga cujo peso real está sendo digitado. */
  protected readonly pesoRealDe = signal<number | null>(null);

  /** O orçamento só muda com a versão em desenvolvimento. */
  protected readonly editavel = computed(
    () =>
      this.podeEditar() && this.podeVerCalculo() && this.versao().status === 'EM_DESENVOLVIMENTO',
  );

  protected readonly preco = rxResource({
    params: () => ({
      projetoId: this.projetoId(),
      versao: this.versao(),
      comCalculo: this.podeVerCalculo(),
    }),
    stream: ({ params }) =>
      this.projetosService.getPrecoDaVersao(params.projetoId, params.versao.id, params.comCalculo),
  });

  /** Ligas ativas do ateliê, só quando o formulário de metais está aberto. */
  protected readonly ligas = rxResource({
    params: () => (this.editando() === 'metais' ? true : undefined),
    stream: () => this.precosService.getPrecos(),
  });

  protected readonly formMetais = new FormGroup({
    linhas: new FormArray<
      FormGroup<{ ligaId: FormControl<number | null>; peso: FormControl<number | null> }>
    >([]),
  });
  protected readonly formMaoDeObra = new FormGroup({
    tipo: new FormControl<TipoMaoDeObra>('POR_HORAS', { nonNullable: true }),
    horas: new FormControl<number | null>(null, [Validators.min(0.01), Validators.max(1000)]),
    valor: new FormControl<number | null>(null, [Validators.min(0.01)]),
  });
  protected readonly formServicos = new FormGroup({
    linhas: new FormArray<
      FormGroup<{ nome: FormControl<string>; valor: FormControl<number | null> }>
    >([]),
  });
  protected readonly formMarkup = new FormGroup({
    markup: new FormControl<number | null>(null, [Validators.min(0.01), Validators.max(10)]),
  });
  protected readonly formPesoReal = new FormGroup({
    peso: new FormControl<number | null>(null, [Validators.min(0.001)]),
  });

  protected linhaDoMetal(m: CustoDoMetalDto, c: CalculoDoPrecoDto): string {
    return linhaDoMetal(m, c.perdaPercentual);
  }

  protected linhaDaComposicao(c: CalculoDoPrecoDto): string {
    if (c.composicao.length === 0) return 'Composição: nenhuma peça';
    const unidades = c.composicao.reduce((total, p) => total + p.quantidade, 0);
    const rotulo = unidades === 1 ? 'unidade' : 'unidades';
    return `Composição: ${unidades} ${rotulo}`;
  }

  protected linhaDaMaoDeObra(c: CalculoDoPrecoDto): string {
    const m = c.maoDeObra;
    if (m.tipo === 'POR_HORAS')
      return `Mão de obra: ${numero(m.horas ?? 0)} h × ${m.valorHora ? moeda(m.valorHora) : 'hora a definir'}`;
    return m.tipo === 'VALOR_FIXO' ? 'Mão de obra: valor fixo' : 'Mão de obra: não informada';
  }

  protected linhaDosServicos(c: CalculoDoPrecoDto): string {
    if (c.servicos.length === 0) return 'Serviços: nenhum';
    const itens = c.servicos.map((s) => [s.nome, moeda(s.valor)].join(' '));
    return `Serviços: ${itens.join(' + ')}`;
  }

  /** "× 1,8 × 1,06, arredondado". */
  protected linhaDoPreco(c: CalculoDoPrecoDto): string {
    const markup = `× ${numero(c.markup)}${c.markupDaVersao !== null ? ' (da versão)' : ''}`;
    const taxas = c.taxasPercentual > 0 ? ` × ${numero(1 + c.taxasPercentual / 100)}` : '';
    return `${markup}${taxas}${c.arredondarPara > 0 ? ', arredondado' : ''}`;
  }

  /** Pendência que se resolve aqui mesmo, no formulário de metais (só sem 3D: com 3D, vale o modelo). */
  protected resolveNosMetais(p: PendenciaDoPrecoDto, c: CalculoDoPrecoDto): boolean {
    return (
      c.origemDoPeso === 'MANUAL' && (p.tipo === 'SEM_METAL_COM_PESO' || p.tipo === 'LIGA_DESATIVADA')
    );
  }

  /** Pendência que se resolve no modelo 3D (material dos corpos, a medição, as ligas do modelo). */
  protected resolveNo3d(p: PendenciaDoPrecoDto, c: CalculoDoPrecoDto): boolean {
    return (
      p.tipo === 'CORPOS_SEM_MATERIAL' ||
      p.tipo === 'MODELO_NAO_MEDIDO' ||
      (c.origemDoPeso === 'MODELO' && (p.tipo === 'SEM_METAL_COM_PESO' || p.tipo === 'LIGA_DESATIVADA'))
    );
  }

  /** "Peso da peça (3D) · medido no modelo" ou "Peso estimado manualmente". */
  protected origemDoPeso(c: CalculoDoPrecoDto): string {
    return c.origemDoPeso === 'MODELO'
      ? 'Peso da peça (3D) · medido no modelo'
      : 'Peso estimado manualmente';
  }

  /** "Ouro 18k amarelo 5,00 g, Prata 925 1,20 g". */
  protected pesosManuais(c: CalculoDoPrecoDto): string {
    return c.pesosManuaisGuardados.map((p) => `${p.liga} ${gramas(p.peso)}`).join(', ');
  }

  /** O peso real é por liga da versão: com 3D, só das ligas que têm peso manual guardado. */
  protected registraPesoReal(m: CustoDoMetalDto, c: CalculoDoPrecoDto): boolean {
    return (
      c.origemDoPeso === 'MANUAL' || c.pesosManuaisGuardados.some((p) => p.ligaId === m.ligaId)
    );
  }

  protected iconeDoAviso(tipo: TipoDeAviso): string {
    return ICONE_DO_AVISO[tipo];
  }

  protected abrir(edicao: Exclude<Edicao, null>, c: CalculoDoPrecoDto): void {
    this.pesoRealDe.set(null);
    switch (edicao) {
      case 'metais': {
        // Com 3D, edita o peso manual guardado (não o medido).
        const manuais =
          c.origemDoPeso === 'MODELO'
            ? c.pesosManuaisGuardados.map((p) => ({ ligaId: p.ligaId, peso: p.peso }))
            : c.metais.map((m) => ({ ligaId: m.ligaAtiva ? m.ligaId : null, peso: m.pesoEstimado }));
        this.formMetais.controls.linhas.clear();
        manuais.forEach((m) => this.adicionarMetal(m.ligaId, m.peso));
        if (manuais.length === 0) this.adicionarMetal(null, null);
        break;
      }
      case 'maoDeObra':
        this.formMaoDeObra.reset({
          tipo: c.maoDeObra.tipo ?? 'POR_HORAS',
          horas: c.maoDeObra.horas,
          valor: c.maoDeObra.tipo === 'VALOR_FIXO' ? c.maoDeObra.custo : null,
        });
        break;
      case 'servicos':
        this.formServicos.controls.linhas.clear();
        c.servicos.forEach((s) => this.adicionarServico(s.nome, s.valor));
        if (c.servicos.length === 0) this.adicionarServico('', null);
        break;
      case 'markup':
        this.formMarkup.reset({ markup: c.markupDaVersao });
        break;
    }
    this.editando.set(edicao);
  }

  protected adicionarMetal(ligaId: number | null = null, peso: number | null = null): void {
    this.formMetais.controls.linhas.push(
      new FormGroup({
        ligaId: new FormControl<number | null>(ligaId, Validators.required),
        peso: new FormControl<number | null>(peso, [
          Validators.required,
          Validators.min(0.001),
          Validators.max(10000),
        ]),
      }),
    );
  }

  protected adicionarServico(nome = '', valor: number | null = null): void {
    this.formServicos.controls.linhas.push(
      new FormGroup({
        nome: new FormControl(nome, {
          nonNullable: true,
          validators: [Validators.required, Validators.maxLength(80)],
        }),
        valor: new FormControl<number | null>(valor, [Validators.required, Validators.min(0.01)]),
      }),
    );
  }

  protected salvarMetais(): void {
    if (this.invalido(this.formMetais)) return;
    const metais = this.formMetais
      .getRawValue()
      .linhas.map((m) => ({ ligaId: Number(m.ligaId), pesoGramas: m.peso ?? 0 }));
    this.gravar(
      this.projetosService.putMetaisDaVersao(this.projetoId(), this.versao().id, { metais }),
      'Metais da versão salvos.',
    );
  }

  protected salvarMaoDeObra(): void {
    const { tipo, horas, valor } = this.formMaoDeObra.getRawValue();
    const campo =
      tipo === 'POR_HORAS' ? this.formMaoDeObra.controls.horas : this.formMaoDeObra.controls.valor;
    if (campo.value === null) campo.setErrors({ required: true });
    if (this.invalido(campo)) return;
    const dto = tipo === 'POR_HORAS' ? { tipo, horas, valor: null } : { tipo, horas: null, valor };
    this.gravar(
      this.projetosService.putMaoDeObra(this.projetoId(), this.versao().id, dto),
      'Mão de obra salva.',
    );
  }

  protected salvarServicos(): void {
    if (this.invalido(this.formServicos)) return;
    const servicos = this.formServicos
      .getRawValue()
      .linhas.map((s) => ({ nome: s.nome.trim(), valor: s.valor ?? 0 }));
    this.gravar(
      this.projetosService.putServicos(this.projetoId(), this.versao().id, { servicos }),
      'Serviços salvos.',
    );
  }

  /** Vazio volta a valer o markup do projeto. */
  protected salvarMarkup(): void {
    if (this.invalido(this.formMarkup)) return;
    this.gravar(
      this.projetosService.putMarkupDaVersao(this.projetoId(), this.versao().id, {
        markup: this.formMarkup.controls.markup.value,
      }),
      'Markup da versão salvo.',
    );
  }

  protected abrirPesoReal(m: CustoDoMetalDto): void {
    this.editando.set(null);
    this.formPesoReal.reset({ peso: m.pesoReal });
    this.pesoRealDe.set(m.ligaId);
  }

  /** Vazio apaga o peso real. */
  protected salvarPesoReal(ligaId: number): void {
    if (this.invalido(this.formPesoReal)) return;
    this.gravar(
      this.projetosService.putPesoReal(this.projetoId(), this.versao().id, ligaId, {
        pesoGramas: this.formPesoReal.controls.peso.value,
      }),
      'Peso real registrado.',
    );
  }

  protected cancelar(): void {
    this.editando.set(null);
    this.pesoRealDe.set(null);
  }

  private invalido(controle: FormGroup | FormControl): boolean {
    controle.markAllAsTouched();
    return controle.invalid;
  }

  /** Grava, fecha o formulário e recalcula (erros 400/422 já viram toast no interceptor). */
  private gravar(comando: Observable<void>, sucesso: string): void {
    this.salvando.set(true);
    comando.pipe(finalize(() => this.salvando.set(false))).subscribe(() => {
      this.toast.sucesso(sucesso);
      this.cancelar();
      this.preco.reload();
    });
  }
}

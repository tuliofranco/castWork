import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { PrecosService } from '@data/precificacao/precos/precos.service';
import type { ArquivoFinalDaPecaDto, PecaDoProjetoDto, PesoDaPecaDto, PostPecaDoProjetoDto } from '@data/projetos/projetos/dto/peca-do-projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { TIPOS_PECA, type TipoPeca, formatarTeor } from '@shared/pecas/tipo-peca';
import { ToastService } from '@shared/services/toast.service';
import { BarraAcoesComponent } from '@shared/ui/barra-acoes.component';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoComponent } from '@shared/ui/campo.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { PainelPdfComponent } from '@shared/ui/painel-pdf.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { type Observable, finalize, map, of } from 'rxjs';

import { STATUS_PECA_DO_PROJETO } from '../status-peca-do-projeto';

/** Campos que definem a peça: mudar algum deles (inclusive trocar a liga) numa peça publicada faz dela outra peça. */
const CAMPOS_DA_PECA = ['nome', 'tipo', 'ligaId', 'material', 'pesoGramas', 'descricao'] as const;

/** Uma opção do seletor de liga: as ligas ativas e, na edição, a gravada que foi desativada depois. */
interface OpcaoDeLiga {
  readonly id: number;
  readonly nome: string;
  readonly teor: number | null;
  /** Nula na liga desativada gravada na peça: a peça só guarda nome e teor. */
  readonly metal: 'OURO' | 'PRATA' | null;
}

/**
 * Peça do projeto: cria (rota `pecas/nova`) ou abre (`pecas/:pecaId`) uma peça do projeto. Na peça aberta: os dados
 * (com aviso antes de salvar quando ela está publicada e algum dado mudou, porque vira outra peça), o valor no projeto
 * (ver com `precificacao.ler`, definir com `precificacao.editar`) e Publicar na biblioteca (rascunho). Os dados e a
 * publicação pedem `projetos.editar`; sem a regra, a página é só leitura.
 *
 * O material é a **liga** do ateliê (seletor com as ligas ativas; o teor vem dela) em todo tipo menos pedra, e texto
 * livre na pedra. A peça guarda uma cópia do nome e do teor da liga: a liga gravada que foi desativada continua como
 * opção, e salvar sem trocar a liga mantém a cópia.
 *
 * Na peça aberta: **Ferramentas da peça** (o desenho 2D e o modelo 3D só dela, como as ferramentas do projeto) e de
 * onde vem o peso: com modelo 3D final, o medido nele na liga da peça; sem 3D final, o digitado. Cada cartão mostra o arquivo final da ferramenta.
 *
 * **Documentos**: a ficha técnica da peça em PDF, gerada na hora pelo servidor; Ver abre no painel de tela cheia e
 * Baixar salva o arquivo.
 */
@Component({
  selector: 'cw-peca-do-projeto',
  imports: [CurrencyPipe, DecimalPipe, ReactiveFormsModule, RouterLink, CabecalhoPaginaComponent, CampoComponent, BarraAcoesComponent, EstadoListaComponent, PainelPdfComponent, SeloComponent],
  templateUrl: './peca-do-projeto.component.html',
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
    .opcao:has(input:disabled) { cursor: default; }
    .situacao { display: flex; flex-wrap: wrap; align-items: center; gap: 0.5rem; margin: 0 0 1.5rem; color: var(--cw-ink-muted); }
    .bloco { margin: 0 0 2rem; padding: 1rem 1.25rem; border: 1px solid var(--cw-border); border-radius: var(--cw-radius); background: var(--cw-surface); }
    .bloco h2 { margin: 0 0 0.75rem; font-size: 1.0625rem; font-weight: 600; }
    .linha-valor { display: flex; flex-wrap: wrap; align-items: flex-end; gap: 0.75rem; }
    .linha-valor cw-campo { flex: 0 1 14rem; margin: 0; }
    .valor-atual { font-variant-numeric: tabular-nums; font-weight: 600; }
    .confirmar { display: flex; flex-wrap: wrap; align-items: center; gap: 0.75rem; margin: 0 0 1rem; padding: 0.875rem 1rem; border-radius: var(--cw-radius); background: var(--cw-warning-soft); color: var(--cw-warning-ink); }
    .confirmar .botoes { display: flex; gap: 0.5rem; margin-left: auto; }
    .ativa { display: inline-flex; align-items: center; gap: 0.5rem; min-height: var(--cw-alvo); font-weight: 600; }
    .peso { margin: 0.5rem 0 0; color: var(--cw-ink-muted); }
    .peso strong { color: var(--cw-ink); font-variant-numeric: tabular-nums; }
    /* Mesmo cartão das ferramentas do projeto e da versão. */
    .lista-ferramentas { display: flex; flex-wrap: wrap; gap: 0.75rem; }
    .ferramenta {
      display: flex; align-items: center; gap: 0.75rem; min-height: 3.5rem; padding: 0.625rem 1rem 0.625rem 0.75rem;
      border: 1px solid var(--cw-border); border-radius: var(--cw-radius-lg); color: var(--cw-ink); text-decoration: none;
    }
    .ferramenta > .bi {
      display: grid; place-items: center; width: 2.5rem; height: 2.5rem; border-radius: var(--cw-radius);
      background: var(--cw-accent-soft); color: var(--cw-accent-ink); font-size: 1.125rem;
    }
    .ferramenta span { display: flex; flex-direction: column; }
    .ferramenta small { color: var(--cw-ink-muted); font-size: 0.8125rem; }
    .ferramenta .final { color: var(--cw-ink); font-weight: 600; }
    .ferramenta .sem-final { color: var(--cw-warning-ink); font-weight: 600; }
    .ferramenta:hover { background: var(--cw-surface-hover); }
    .documento { display: flex; flex-wrap: wrap; align-items: center; gap: 0.5rem; }
    .documento > .bi { color: var(--cw-ink-muted); }
    .documento .texto { flex: 1 1 auto; font-weight: 600; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PecaDoProjetoComponent {
  private readonly projetosService = inject(ProjetosService);
  private readonly precosService = inject(PrecosService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly permissao = inject(PermissaoService);

  /** `:id` do projeto e `:pecaId` da peça (withComponentInputBinding); sem `pecaId`, é o cadastro. */
  readonly id = input.required<string>();
  readonly pecaId = input<string>();

  protected readonly tipos = TIPOS_PECA;
  protected readonly status = STATUS_PECA_DO_PROJETO;
  protected readonly podeEditar = this.permissao.temAlguma(['projetos.editar']);
  protected readonly podeVerValor = this.permissao.temAlguma(['precificacao.ler']);
  protected readonly podeEditarValor = this.permissao.temAlguma(['precificacao.editar']);
  protected readonly editando = computed(() => !!this.pecaId());
  protected readonly voltarPara = computed(() => `/projetos/${this.id()}`);

  // A liga e o peso são validados no servidor; aqui só o essencial. `material` é só da pedra; `ligaId`, dos outros tipos.
  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    tipo: ['ELO' as TipoPeca, Validators.required],
    ligaId: [null as number | null],
    material: ['', Validators.maxLength(60)],
    pesoGramas: [null as number | null, Validators.min(0.001)],
    descricao: ['', Validators.maxLength(2000)],
    ativo: [true],
  });

  /** Valor no projeto: R$ por unidade, nunca negativo. Vazio tira o valor. */
  protected readonly formValor = new FormGroup({ valor: new FormControl<number | null>(null, Validators.min(0)) });
  protected readonly valor = this.formValor.controls.valor;

  protected readonly peca = rxResource({
    params: () => ({ projetoId: Number(this.id()), pecaId: this.pecaId() ? Number(this.pecaId()) : null }),
    stream: ({ params }): Observable<PecaDoProjetoDto | null | undefined> =>
      params.pecaId === null
        ? of(undefined)
        : this.projetosService.getPecasDoProjeto(params.projetoId, this.podeVerValor).pipe(map((pecas) => pecas?.find((p) => p.id === params.pecaId) ?? null)),
  });

  /** O peso que vale e de onde vem (com modelo 3D, o servidor mede o arquivo da peça). */
  protected readonly peso = rxResource({
    params: () => (this.pecaId() ? { projetoId: Number(this.id()), pecaId: Number(this.pecaId()) } : undefined),
    stream: ({ params }): Observable<PesoDaPecaDto | null> => (params ? this.projetosService.getPesoDaPeca(params.projetoId, params.pecaId) : of(null)),
  });

  /** O arquivo final de cada ferramenta (o que vale para o peso, a montagem e a publicação). */
  protected readonly finais = rxResource({
    params: () => (this.pecaId() ? { projetoId: Number(this.id()), pecaId: Number(this.pecaId()) } : undefined),
    stream: ({ params }): Observable<readonly ArquivoFinalDaPecaDto[]> => (params ? this.projetosService.getArquivosFinaisDaPeca(params.projetoId, params.pecaId) : of([])),
  });

  /** A ficha técnica aberta no painel de tela cheia. */
  protected readonly fichaAberta = signal<Blob | null>(null);
  protected readonly gerandoFicha = signal(false);

  protected readonly ligas = rxResource({ stream: () => this.precosService.getLigas() });

  private readonly tipo = toSignal(this.form.controls.tipo.valueChanges, { initialValue: this.form.controls.tipo.value });
  private readonly ligaId = toSignal(this.form.controls.ligaId.valueChanges, { initialValue: this.form.controls.ligaId.value });
  protected readonly ehPedra = computed(() => this.tipo() === 'PEDRA');

  /** As ligas ativas e, se a gravada foi desativada, ela também (com o nome e o teor copiados na peça). */
  protected readonly opcoesDeLiga = computed<readonly OpcaoDeLiga[]>(() => {
    const ativas: OpcaoDeLiga[] = (this.ligas.value() ?? []).map((l) => ({ id: l.id, nome: l.nome, teor: l.teor, metal: l.metal }));
    const peca = this.peca.value();
    if (!peca || peca.ligaId === null || ativas.some((l) => l.id === peca.ligaId)) return ativas;
    return [...ativas, { id: peca.ligaId, nome: `${peca.material ?? 'Liga'} (desativada)`, teor: peca.teor, metal: null }];
  });

  /** Peça de metal anterior à liga: ficou com o material em texto, sem liga; salvar sem escolher uma liga o mantém. */
  protected readonly materialSemLiga = computed(() => {
    const peca = this.peca.value();
    return peca && peca.tipo !== 'PEDRA' && peca.ligaId === null && peca.material ? peca.material : null;
  });

  /** O teor vem da liga: mostrado como dica do seletor. */
  protected readonly dicaDaLiga = computed(() => {
    const id = this.ligaId();
    const opcao = this.opcoesDeLiga().find((l) => l.id === id);
    if (opcao) return `Teor ${formatarTeor(opcao.teor, opcao.metal)}, da liga.`;
    const peca = this.peca.value();
    return id === null && this.materialSemLiga() && peca?.teor ? `Teor ${formatarTeor(peca.teor)}.` : 'O teor vem da liga.';
  });

  protected readonly enviando = signal(false);
  protected readonly errosGerais = signal<readonly string[]>([]);
  /** Aviso antes de salvar dados alterados de uma peça publicada. */
  protected readonly confirmandoOutraPeca = signal(false);

  constructor() {
    effect(() => {
      const peca = this.peca.value();
      if (peca) this.preencher(peca);
    });
    if (!this.podeEditar) this.form.disable();
    if (!this.podeEditarValor) this.valor.disable();
  }

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const peca = this.peca.value();
    if (peca?.status === 'PUBLICADA' && this.dadosMudaram(peca) && !this.confirmandoOutraPeca()) {
      this.confirmandoOutraPeca.set(true);
      return;
    }
    this.confirmandoOutraPeca.set(false);

    const valor = this.form.getRawValue();
    const pedra = valor.tipo === 'PEDRA';
    const dto: PostPecaDoProjetoDto = {
      nome: valor.nome,
      tipo: valor.tipo,
      ligaId: pedra ? null : valor.ligaId,
      material: pedra ? valor.material || null : null,
      pesoGramas: valor.pesoGramas,
      descricao: valor.descricao || null,
    };
    const projetoId = Number(this.id());
    const envio: Observable<unknown> = peca
      ? this.projetosService.putPecaDoProjeto(projetoId, peca.id, { ...dto, ativo: valor.ativo })
      : this.projetosService.postPecaDoProjeto(projetoId, dto);
    this.enviando.set(true);
    this.errosGerais.set([]);

    envio.pipe(finalize(() => this.enviando.set(false))).subscribe({
      next: (resposta) => {
        if (peca) {
          this.toast.sucesso('Peça alterada.');
          this.peca.reload();
        } else {
          this.toast.sucesso('Peça criada no projeto, em rascunho.');
          void this.router.navigate(['/projetos', projetoId, 'pecas', (resposta as { id: number }).id]);
        }
      },
      error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
    });
  }

  /** De onde vem o peso, como a tela diz. */
  protected origemDoPeso(peso: PesoDaPecaDto): string {
    switch (peso.origem) {
      case 'MODELO':
        return peso.pesoDigitado === null
          ? 'medido no modelo 3D, na liga da peça'
          : 'medido no modelo 3D, na liga da peça (o peso digitado fica guardado e não é usado)';
      case 'MODELO_SEM_LIGA':
        return 'digitado: a peça tem modelo 3D, mas não tem liga para medir';
      case 'MODELO_NAO_MEDIDO':
        return 'não foi possível medir o modelo 3D da peça';
      default:
        return 'digitado (a peça não tem modelo 3D final)';
    }
  }

  protected salvarValor(): void {
    const peca = this.peca.value();
    if (!peca || this.valor.invalid) {
      this.valor.markAsTouched();
      return;
    }
    const valor = this.valor.value;
    this.enviando.set(true);
    this.projetosService
      .putValorDaPeca(Number(this.id()), peca.id, { valor })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          this.toast.sucesso(valor === null ? 'Valor retirado.' : 'Valor definido.');
          this.peca.reload();
        },
        error: () => this.peca.reload(),
      });
  }

  protected publicar(): void {
    const peca = this.peca.value();
    if (!peca) return;
    this.enviando.set(true);
    this.projetosService
      .postPublicarPeca(Number(this.id()), peca.id)
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          this.toast.sucesso(`${peca.nome} publicada na biblioteca.`);
          this.peca.reload();
        },
        error: () => this.peca.reload(),
      });
  }

  /** Gera a ficha técnica e abre no painel. */
  protected verFichaTecnica(): void {
    this.gerarFichaTecnica((pdf) => this.fichaAberta.set(pdf));
  }

  /** O arquivo final da ferramenta no cartão: o nome dado ou a data; sem final, "Sem arquivo final". */
  protected descreverFinal(ferramenta: string): string {
    const final = this.finais.value()?.find((f) => f.ferramenta === ferramenta);
    if (!final) return 'Sem arquivo final';
    const quando = new Date(final.criadoEm).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
    return `Final: ${final.nome ?? quando}`;
  }

  protected temFinal(ferramenta: string): boolean {
    return !!this.finais.value()?.some((f) => f.ferramenta === ferramenta);
  }

  /** Gera a ficha técnica e salva o arquivo. */
  protected baixarFichaTecnica(): void {
    this.gerarFichaTecnica((pdf) => this.salvarArquivo(pdf));
  }

  /** Salva a ficha que já está aberta no painel, sem gerar de novo. */
  protected salvarArquivo(pdf: Blob): void {
    const url = URL.createObjectURL(pdf);
    const link = document.createElement('a');
    link.href = url;
    link.download = `ficha-tecnica-${this.nomeDoArquivo()}.pdf`;
    link.click();
    URL.revokeObjectURL(url);
  }

  private gerarFichaTecnica(usar: (pdf: Blob) => void): void {
    const peca = this.peca.value();
    if (!peca) return;
    this.gerandoFicha.set(true);
    this.projetosService
      .getFichaTecnicaDaPeca(Number(this.id()), peca.id)
      .pipe(finalize(() => this.gerandoFicha.set(false)))
      .subscribe(usar);
  }

  /** Nome da peça sem acentos nem espaços, para o arquivo baixado ("Elo oval" → "elo-oval"). */
  private nomeDoArquivo(): string {
    const nome = this.peca.value()?.nome ?? 'peca';
    return (
      nome
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-|-$/g, '') || 'peca'
    );
  }

  /** Compara o que será enviado: o material livre só conta na pedra, e a liga só fora dela. */
  private dadosMudaram(peca: PecaDoProjetoDto): boolean {
    const salvo = this.valoresDoFormulario(peca);
    const atual = { ...this.form.getRawValue() };
    if (atual.tipo === 'PEDRA') atual.ligaId = null;
    else atual.material = '';
    return CAMPOS_DA_PECA.some((campo) => String(atual[campo] ?? '').trim() !== String(salvo[campo] ?? '').trim());
  }

  private valoresDoFormulario(peca: PecaDoProjetoDto) {
    const pedra = peca.tipo === 'PEDRA';
    return {
      nome: peca.nome,
      tipo: peca.tipo,
      ligaId: pedra ? null : peca.ligaId,
      material: pedra ? (peca.material ?? '') : '',
      pesoGramas: peca.pesoGramas,
      descricao: peca.descricao ?? '',
      ativo: peca.ativo,
    };
  }

  private preencher(peca: PecaDoProjetoDto): void {
    this.form.reset(this.valoresDoFormulario(peca));
    this.valor.reset(peca.valor ?? null);
    this.confirmandoOutraPeca.set(false);
  }
}

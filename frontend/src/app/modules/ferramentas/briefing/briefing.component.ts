import { valorDe } from '@shared/recursos/valor-de';
import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PermissaoService } from '@core/services/permissao.service';
import { BriefingService } from '@data/ferramentas/briefing/briefing.service';
import type { BriefingDto, TipoJoia } from '@data/ferramentas/briefing/dto/briefing.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { ToastService } from '@shared/services/toast.service';
import { BarraAcoesComponent } from '@shared/ui/barra-acoes.component';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoComponent } from '@shared/ui/campo.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { PainelPdfComponent } from '@shared/ui/painel-pdf.component';
import { VisualizadorPdfComponent } from '@shared/ui/visualizador-pdf.component';
import { finalize, firstValueFrom, switchMap } from 'rxjs';

import { ConversaIaComponent } from '../../assistente/conversa-ia.component';
import { MATERIAIS_COMUNS, TIPOS_JOIA } from '../../projetos/tipo-joia';

/**
 * Briefing: a ferramenta do projeto que escreve o documento. Antes de tudo, o formulário inicial; depois, a
 * tela padrão das ferramentas com IA (\`cw-conversa-ia\`), com o PDF ao vivo na área de trabalho. A IA grava os
 * campos e gera o PDF pela conversa; o joalheiro não edita o documento à mão. Vale para todas as versões.
 */
@Component({
  selector: 'cw-briefing',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    CabecalhoPaginaComponent,
    CampoComponent,
    BarraAcoesComponent,
    EstadoListaComponent,
    PainelPdfComponent,
    VisualizadorPdfComponent,
    ConversaIaComponent,
  ],
  templateUrl: './briefing.component.html',
  styleUrl: './briefing.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BriefingComponent {
  protected readonly valorDe = valorDe;
  private readonly briefingService = inject(BriefingService);
  private readonly projetosService = inject(ProjetosService);
  private readonly toast = inject(ToastService);

  /** \`:id\` da rota (withComponentInputBinding). */
  readonly id = input.required<string>();

  /** O briefing é do projeto: o voltar leva sempre ao projeto, onde ficam as ferramentas do projeto. */
  protected readonly voltarPara = computed(() => `/projetos/${this.id()}`);

  protected readonly podeEditar = inject(PermissaoService).temAlguma(['projetos.editar']);
  protected readonly tipos = TIPOS_JOIA;
  protected readonly materiaisComuns = MATERIAIS_COMUNS;

  protected readonly projeto = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.projetosService.getProjeto(params),
  });

  protected readonly briefing = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.briefingService.getBriefing(params),
  });

  protected readonly projetoNumero = computed(() => Number(this.id()));
  protected readonly editandoFormulario = signal(false);
  protected readonly salvandoFormulario = signal(false);
  protected readonly errosGerais = signal<readonly string[]>([]);
  protected readonly gerandoPdf = signal(false);
  /**
   * O PDF da área de trabalho: o último guardado (o padrão). Sem nenhum guardado, a prévia de como está agora.
   * Depende do briefing carregado, então volta a aparecer ao reabrir a conversa.
   */
  protected readonly pdfAtual = rxResource({
    params: () => {
      const b = valorDe(this.briefing);
      return b ? { projetoId: b.projetoId, documentoId: b.documentos[0]?.id ?? null } : undefined;
    },
    stream: ({ params }) =>
      params.documentoId !== null
        ? this.briefingService.getDocumento(params.projetoId, params.documentoId)
        : this.briefingService.getPrevia(params.projetoId),
  });
  /** Um PDF guardado aberto em tela cheia. */
  protected readonly visualizando = signal<{ readonly arquivo: Blob; readonly titulo: string } | null>(null);
  protected readonly mostrandoDocumentos = signal(false);

  protected readonly mostrarFormulario = computed(() => valorDe(this.briefing) === null || this.editandoFormulario());

  protected readonly form = inject(FormBuilder).nonNullable.group({
    tipoJoia: ['Anel' as TipoJoia, Validators.required],
    oQueOClienteQuer: ['', [Validators.required, Validators.maxLength(2000)]],
    ocasiao: ['', Validators.maxLength(300)],
    materiais: ['', Validators.maxLength(300)],
    medidas: ['', Validators.maxLength(300)],
    prazo: [''],
    orcamento: ['', Validators.maxLength(300)],
  });

  protected materialMarcado(material: string): boolean {
    return this.listaDeMateriais().includes(material);
  }

  protected alternarMaterial(material: string): void {
    const lista = this.listaDeMateriais();
    const nova = lista.includes(material) ? lista.filter((m) => m !== material) : [...lista, material];
    this.form.controls.materiais.setValue(nova.join(', '));
  }

  protected editarFormulario(b: BriefingDto): void {
    this.form.setValue({
      tipoJoia: b.tipoJoia,
      oQueOClienteQuer: b.oQueOClienteQuer,
      ocasiao: b.ocasiao ?? '',
      materiais: b.materiais ?? '',
      medidas: b.medidas ?? '',
      prazo: b.prazo ?? '',
      orcamento: b.orcamento ?? '',
    });
    this.errosGerais.set([]);
    this.editandoFormulario.set(true);
  }

  protected salvarFormulario(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const primeiraVez = valorDe(this.briefing) === null;
    this.salvandoFormulario.set(true);
    this.errosGerais.set([]);
    this.briefingService
      .putFormulario(this.projetoId(), {
        tipoJoia: v.tipoJoia,
        oQueOClienteQuer: v.oQueOClienteQuer,
        ocasiao: v.ocasiao || null,
        materiais: v.materiais || null,
        medidas: v.medidas || null,
        prazo: v.prazo || null,
        orcamento: v.orcamento || null,
      })
      // Relê o briefing antes de sair do formulário: a tela troca direto para a conversa, sem piscar.
      .pipe(
        switchMap(() => this.briefingService.getBriefing(this.projetoId())),
        finalize(() => this.salvandoFormulario.set(false)),
      )
      .subscribe({
        next: (atualizado) => {
          this.briefing.set(atualizado);
          this.editandoFormulario.set(false);
          // Na primeira vez, a conversa abre sozinha: a IA resume o formulário e pergunta o que falta.
          if (!primeiraVez) this.toast.sucesso('Formulário atualizado. A IA vai considerar a correção.');
        },
        error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
      });
  }

  /** A IA respondeu: os campos e os documentos podem ter mudado, e o PDF da área de trabalho também. */
  protected aposResposta(): void {
    // Relê por fora do recurso: se a leitura falhar, o briefing que está na tela continua (um `reload()` com erro o apagaria).
    this.briefingService.getBriefing(this.projetoId()).subscribe({
      next: (briefing) => this.briefing.set(briefing),
      error: () => undefined,
    });
    this.pdfAtual.reload();
  }

  /** Guarda o PDF como está e o define como padrão da área de trabalho. */
  protected salvarPdf(): void {
    this.gerandoPdf.set(true);
    this.briefingService
      .postPdf(this.projetoId())
      .pipe(
        switchMap(() => this.briefingService.getBriefing(this.projetoId())),
        finalize(() => this.gerandoPdf.set(false)),
      )
      .subscribe((briefing) => {
        this.briefing.set(briefing);
        this.toast.sucesso('PDF definido como padrão.');
      });
  }

  protected async ver(documentoId: number, geradoEm: string): Promise<void> {
    const arquivo = await firstValueFrom(this.briefingService.getDocumento(this.projetoId(), documentoId));
    const titulo = `Briefing de ${new Date(geradoEm).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}`;
    this.visualizando.set({ arquivo, titulo });
  }

  private listaDeMateriais(): string[] {
    return this.form.controls.materiais.value
      .split(',')
      .map((m) => m.trim())
      .filter((m) => m.length > 0);
  }

  private projetoId(): number {
    return Number(this.id());
  }
}

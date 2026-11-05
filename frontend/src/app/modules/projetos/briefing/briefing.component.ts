import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, afterRenderEffect, computed, inject, input, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { PermissaoService } from '@core/services/permissao.service';
import { BriefingService } from '@data/projetos/briefing/briefing.service';
import type { BriefingDto, MensagemBriefingDto, TipoJoia } from '@data/projetos/briefing/dto/briefing.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { ToastService } from '@shared/services/toast.service';
import { BarraAcoesComponent } from '@shared/ui/barra-acoes.component';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoComponent } from '@shared/ui/campo.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { VisualizadorPdfComponent } from '@shared/ui/visualizador-pdf.component';
import { finalize, map, switchMap } from 'rxjs';

import { MATERIAIS_COMUNS, TIPOS_JOIA } from '../tipo-joia';

/**
 * Briefing do projeto: formulário inicial, conversa com a IA (que consolida o briefing) e os PDFs gerados.
 * O joalheiro não edita o documento: tudo passa pelo formulário e pela conversa.
 */
@Component({
  selector: 'cw-briefing',
  imports: [
    DatePipe,
    FormsModule,
    ReactiveFormsModule,
    CabecalhoPaginaComponent,
    CampoComponent,
    BarraAcoesComponent,
    EstadoListaComponent,
    VisualizadorPdfComponent,
  ],
  templateUrl: './briefing.component.html',
  styleUrl: './briefing.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BriefingComponent {
  private readonly briefingService = inject(BriefingService);
  private readonly projetosService = inject(ProjetosService);
  private readonly toast = inject(ToastService);

  /** `:id` da rota (withComponentInputBinding). */
  readonly id = input.required<string>();

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

  protected readonly editandoFormulario = signal(false);
  protected readonly salvandoFormulario = signal(false);
  protected readonly errosGerais = signal<readonly string[]>([]);
  protected readonly pensando = signal(false);
  protected readonly gerandoPdf = signal(false);
  protected readonly fala = signal('');

  /**
   * Documento aberto no painel de pré-visualização: a prévia (ainda não salva) ou um PDF já guardado.
   */
  protected readonly visualizando = signal<{ readonly arquivo: Blob; readonly titulo: string; readonly salvo: { id: number; nome: string } | null } | null>(null);
  protected readonly abrindoPrevia = signal(false);

  /** Fala do joalheiro já enviada, mostrada enquanto a IA responde. */
  protected readonly falaPendente = signal<string | null>(null);

  protected readonly mostrarFormulario = computed(() => this.briefing.value() === null || this.editandoFormulario());

  protected readonly resumo = computed(() => {
    const b = this.briefing.value();
    if (!b) return [];
    const linhas: [string, string | null][] = [
      ['Tipo de peça', b.tipoJoia],
      ['Medidas', b.medidas],
      ['Materiais', b.materiais],
      ['Acabamento', b.acabamento],
      ['Pedras', b.pedras],
      ['Fecho', b.fecho],
      ['Ocasião', b.ocasiao],
      ['Prazo', b.prazo ? new Date(`${b.prazo}T12:00:00`).toLocaleDateString('pt-BR') : null],
      ['Orçamento', b.orcamento],
      ['Observações', b.observacoes],
    ];
    return linhas.filter((l): l is [string, string] => !!l[1]);
  });

  protected readonly form = inject(FormBuilder).nonNullable.group({
    tipoJoia: ['Anel' as TipoJoia, Validators.required],
    oQueOClienteQuer: ['', [Validators.required, Validators.maxLength(2000)]],
    ocasiao: ['', Validators.maxLength(300)],
    materiais: ['', Validators.maxLength(300)],
    medidas: ['', Validators.maxLength(300)],
    prazo: [''],
    orcamento: ['', Validators.maxLength(300)],
  });

  private readonly fimDaConversa = viewChild<ElementRef<HTMLElement>>('fimDaConversa');
  private readonly painel = viewChild<ElementRef<HTMLDialogElement>>('painel');

  constructor() {
    // Mantém a última mensagem à vista quando a conversa cresce.
    afterRenderEffect(() => {
      this.briefing.value();
      this.falaPendente();
      this.fimDaConversa()?.nativeElement.scrollIntoView({ block: 'end' });
    });
  }

  protected paragrafos(mensagem: MensagemBriefingDto): string[] {
    // A IA escreve texto simples; tira marcações que escaparem em vez de interpretar HTML.
    return mensagem.texto
      .replace(/\*\*|__/g, '')
      .split(/\n{2,}/)
      .map((p) => p.trim())
      .filter((p) => p.length > 0);
  }

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
    const primeiraVez = this.briefing.value() === null;
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
          if (primeiraVez) {
            // A IA já abre a conversa com o resumo e as primeiras perguntas.
            this.enviar(null);
          } else {
            this.toast.sucesso('Formulário atualizado. A IA vai considerar a correção.');
          }
        },
        error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
      });
  }

  protected enviarFala(): void {
    const texto = this.fala().trim();
    if (!texto || this.pensando()) return;
    this.enviar(texto);
  }

  /** Para quando a abertura falhou (IA indisponível): a IA abre a conversa a partir do formulário. */
  protected iniciarConversa(): void {
    this.enviar(null);
  }

  protected teclaNaFala(evento: KeyboardEvent): void {
    // Enter envia; Shift+Enter quebra a linha (teclado físico no tablet).
    if (evento.key === 'Enter' && !evento.shiftKey) {
      evento.preventDefault();
      this.enviarFala();
    }
  }

  protected preVisualizar(): void {
    this.abrindoPrevia.set(true);
    this.briefingService
      .getPrevia(this.projetoId())
      .pipe(finalize(() => this.abrindoPrevia.set(false)))
      .subscribe((arquivo) => this.abrirPainel({ arquivo, titulo: 'Pré-visualização', salvo: null }));
  }

  protected ver(documentoId: number, nomeArquivo: string, geradoEm: string): void {
    this.briefingService.getDocumento(this.projetoId(), documentoId).subscribe((arquivo) =>
      this.abrirPainel({
        arquivo,
        titulo: `Briefing de ${new Date(geradoEm).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}`,
        salvo: { id: documentoId, nome: nomeArquivo },
      }),
    );
  }

  /** Guarda a prévia como novo documento e já baixa o arquivo guardado. */
  protected salvarEBaixar(): void {
    this.gerandoPdf.set(true);
    this.briefingService
      .postPdf(this.projetoId())
      .pipe(
        switchMap(({ id }) => this.briefingService.getBriefing(this.projetoId()).pipe(map((b) => ({ id, briefing: b })))),
        finalize(() => this.gerandoPdf.set(false)),
      )
      .subscribe(({ id, briefing }) => {
        this.briefing.set(briefing);
        const documento = briefing?.documentos.find((d) => d.id === id);
        if (documento) this.baixar(documento.id, documento.nomeArquivo);
        this.toast.sucesso('PDF salvo no projeto.');
        this.fecharPainel();
      });
  }

  protected ajustarNaConversa(): void {
    this.fecharPainel();
    queueMicrotask(() => document.getElementById('fala')?.focus());
  }

  protected fecharPainel(): void {
    this.painel()?.nativeElement.close();
    this.visualizando.set(null);
  }

  protected baixar(documentoId: number, nomeArquivo: string): void {
    this.briefingService.getDocumento(this.projetoId(), documentoId).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = nomeArquivo;
      link.click();
      URL.revokeObjectURL(url);
    });
  }

  private abrirPainel(conteudo: { arquivo: Blob; titulo: string; salvo: { id: number; nome: string } | null }): void {
    this.visualizando.set(conteudo);
    // Diálogo nativo: prende o foco, fecha com Esc e devolve o foco ao botão que abriu.
    queueMicrotask(() => this.painel()?.nativeElement.showModal());
  }

  private enviar(texto: string | null): void {
    this.pensando.set(true);
    this.falaPendente.set(texto);
    this.fala.set('');
    this.briefingService
      .postConversa(this.projetoId(), texto)
      .pipe(
        finalize(() => {
          this.pensando.set(false);
          this.falaPendente.set(null);
        }),
      )
      .subscribe({
        next: (atualizado) => this.briefing.set(atualizado),
        // O interceptor já mostra o erro; devolve a fala ao campo para não perder o que foi escrito.
        error: () => this.fala.set(texto ?? ''),
      });
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

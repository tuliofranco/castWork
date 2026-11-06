import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import type { PecaDto } from '@data/pecas/pecas/dto/peca.dto';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { BriefingService } from '@data/projetos/briefing/briefing.service';
import type { ItemComposicaoDto, VersaoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { formatarTeor, iconeTipoPeca, rotuloTipoPeca } from '@shared/pecas/tipo-peca';
import { DownloadService } from '@shared/services/download.service';
import { ToastService } from '@shared/services/toast.service';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoBuscaComponent } from '@shared/ui/campo-busca.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { PainelPdfComponent } from '@shared/ui/painel-pdf.component';
import { QuantidadeComponent } from '@shared/ui/quantidade.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { type Observable, debounceTime, distinctUntilChanged, finalize, of } from 'rxjs';

import { STATUS_VERSAO } from '../status-versao';
import { ArquivosVersaoComponent } from './arquivos-versao.component';
import { EtapasVersaoComponent } from './etapas-versao.component';

/**
 * Página de uma versão: composição e, enquanto ela estiver em desenvolvimento, a biblioteca ao lado
 * para adicionar peças. Publicar, duplicar e arquivar também estão aqui.
 */
@Component({
  selector: 'cw-versao-detalhe',
  imports: [DecimalPipe, DatePipe, RouterLink, CabecalhoPaginaComponent, CampoBuscaComponent, EstadoListaComponent, PainelPdfComponent, EtapasVersaoComponent, QuantidadeComponent, SeloComponent, ArquivosVersaoComponent],
  templateUrl: './versao-detalhe.component.html',
  styleUrl: './versao-detalhe.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VersaoDetalheComponent {
  private readonly projetosService = inject(ProjetosService);
  private readonly pecasService = inject(PecasService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly briefingService = inject(BriefingService);
  private readonly download = inject(DownloadService);

  /** `:id` e `:versaoId` da rota (withComponentInputBinding). */
  readonly id = input.required<string>();
  readonly versaoId = input.required<string>();

  protected readonly podeEditar = inject(PermissaoService).temAlguma(['projetos.editar']);
  protected readonly status = STATUS_VERSAO;
  protected readonly rotuloTipo = rotuloTipoPeca;
  protected readonly iconeTipo = iconeTipoPeca;
  protected readonly formatarTeor = formatarTeor;
  protected readonly salvando = signal(false);

  /** O briefing do projeto, só para ver aqui (a edição é na página do projeto). */
  protected readonly briefing = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.briefingService.getBriefing(params),
  });

  protected readonly pdfDoBriefing = signal<{ readonly arquivo: Blob; readonly titulo: string; readonly nome: string } | null>(null);

  protected readonly projeto = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.projetosService.getProjeto(params),
  });

  protected readonly versao = computed<VersaoProjetoDto | undefined>(() =>
    this.projeto.value()?.versoes.find((v) => v.id === Number(this.versaoId())),
  );
  protected readonly voltarPara = computed(() => `/projetos/${this.id()}`);
  protected readonly editavel = computed(() => this.podeEditar && this.versao()?.status === 'EM_DESENVOLVIMENTO');

  /** Soma do peso das peças que têm peso informado (quantidade × peso). */
  protected readonly pesoTotal = computed(() =>
    (this.versao()?.itens ?? []).reduce((total, item) => total + item.quantidade * (item.peca.pesoGramas ?? 0), 0),
  );
  protected readonly unidades = computed(() => (this.versao()?.itens ?? []).reduce((total, item) => total + item.quantidade, 0));

  protected readonly buscaPeca = new FormControl('', { nonNullable: true });
  private readonly termoPeca = toSignal(this.buscaPeca.valueChanges.pipe(debounceTime(300), distinctUntilChanged()), { initialValue: '' });
  protected readonly biblioteca = rxResource({
    params: () => ({ termo: this.termoPeca(), editavel: this.editavel() }),
    stream: ({ params }) =>
      params.editavel ? this.pecasService.getPecas({ termo: params.termo || null, primeiros: 12, somenteAtivas: true }) : of(null),
  });

  /** Quantidade escolhida para cada peça da biblioteca antes de tocar em Adicionar. */
  private readonly quantidadesNovas = signal<Readonly<Record<number, number>>>({});

  protected quantidadeNova(peca: PecaDto): number {
    return this.quantidadesNovas()[peca.id] ?? 1;
  }

  protected definirQuantidadeNova(peca: PecaDto, quantidade: number): void {
    this.quantidadesNovas.update((atual) => ({ ...atual, [peca.id]: quantidade }));
  }

  protected adicionar(peca: PecaDto): void {
    const versao = this.versao();
    if (!versao) return;
    const quantidade = this.quantidadeNova(peca);
    this.executar(this.projetosService.postItem(this.projetoId(), versao.id, { pecaId: peca.id, quantidade }), `${quantidade}× ${peca.nome} adicionada.`, () =>
      this.definirQuantidadeNova(peca, 1),
    );
  }

  protected alterarQuantidade(item: ItemComposicaoDto, quantidade: number): void {
    const versao = this.versao();
    if (!versao || quantidade === item.quantidade) return;
    this.executar(this.projetosService.putItem(this.projetoId(), versao.id, item.id, { quantidade, observacao: item.observacao }), null);
  }

  protected remover(item: ItemComposicaoDto): void {
    const versao = this.versao();
    if (versao) this.executar(this.projetosService.deleteItem(this.projetoId(), versao.id, item.id), `${item.peca.nome} removida.`);
  }

  protected publicar(): void {
    const versao = this.versao();
    if (versao) this.executar(this.projetosService.postPublicarVersao(this.projetoId(), versao.id), `Versão ${versao.numero} publicada.`);
  }

  protected desarquivar(): void {
    const versao = this.versao();
    if (versao) this.executar(this.projetosService.postDesarquivarVersao(this.projetoId(), versao.id), `Versão ${versao.numero} de volta ao desenvolvimento.`);
  }

  /** Arquivada some da lista do projeto: volta para ela. */
  protected arquivar(): void {
    const versao = this.versao();
    if (!versao) return;
    this.salvando.set(true);
    this.projetosService
      .postArquivarVersao(this.projetoId(), versao.id)
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe(() => {
        this.toast.sucesso(`Versão ${versao.numero} arquivada.`);
        void this.router.navigateByUrl(this.voltarPara());
      });
  }

  /** A cópia abre na própria página dela. */
  protected duplicar(): void {
    const versao = this.versao();
    if (!versao) return;
    this.salvando.set(true);
    this.projetosService
      .postDuplicarVersao(this.projetoId(), versao.id)
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe(({ id }) => {
        this.toast.sucesso(`Nova versão criada a partir da v${versao.numero}.`);
        void this.router.navigate(['/projetos', this.projetoId(), 'versoes', id]);
      });
  }

  private projetoId(): number {
    return Number(this.id());
  }

  /** Executa o command e relê o projeto (erros 400/422 já viram toast no interceptor). */
  private executar<T>(comando: Observable<T>, sucesso: string | null, depois?: () => void): void {
    this.salvando.set(true);
    comando.pipe(finalize(() => this.salvando.set(false))).subscribe({
      next: () => {
        if (sucesso) this.toast.sucesso(sucesso);
        depois?.();
        this.projeto.reload();
      },
      error: () => this.projeto.reload(),
    });
  }

  protected verBriefing(documentoId: number, nome: string, geradoEm: string): void {
    this.briefingService.getDocumento(Number(this.id()), documentoId).subscribe((arquivo) =>
      this.pdfDoBriefing.set({ arquivo, nome, titulo: `Briefing de ${new Date(geradoEm).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}` }),
    );
  }

  protected baixarBriefing(arquivo: Blob, nome: string): void {
    this.download.salvar(arquivo, nome);
  }
}

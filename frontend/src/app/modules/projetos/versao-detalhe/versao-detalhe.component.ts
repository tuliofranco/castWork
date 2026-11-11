import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { BriefingService } from '@data/ferramentas/briefing/briefing.service';
import type { ItemComposicaoDto, PecaDoProjetoResumoDto, VersaoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
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
import { type Observable, debounceTime, distinctUntilChanged, finalize } from 'rxjs';

import { PecasEmRascunhoComponent, pecasEmRascunho } from '../pecas-em-rascunho.component';
import { STATUS_PECA_DO_PROJETO } from '../status-peca-do-projeto';
import { STATUS_VERSAO } from '../status-versao';
import { ArquivosVersaoComponent } from './arquivos-versao.component';
import { EtapasVersaoComponent } from './etapas-versao.component';
import { PrecoVersaoComponent } from './preco-versao.component';

/**
 * Página de uma versão: composição e, enquanto ela estiver em desenvolvimento, as peças do projeto ao lado para
 * adicionar, e o card do preço. Publicar, duplicar e arquivar também estão aqui; publicar com peça em rascunho mostra
 * as peças que impedem, com atalho para cada uma.
 */
@Component({
  selector: 'cw-versao-detalhe',
  imports: [
    DecimalPipe,
    DatePipe,
    RouterLink,
    CabecalhoPaginaComponent,
    CampoBuscaComponent,
    EstadoListaComponent,
    PainelPdfComponent,
    EtapasVersaoComponent,
    PecasEmRascunhoComponent,
    QuantidadeComponent,
    SeloComponent,
    ArquivosVersaoComponent,
    PrecoVersaoComponent,
  ],
  templateUrl: './versao-detalhe.component.html',
  styleUrl: './versao-detalhe.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VersaoDetalheComponent {
  private readonly projetosService = inject(ProjetosService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly briefingService = inject(BriefingService);
  private readonly download = inject(DownloadService);

  /** `:id` e `:versaoId` da rota (withComponentInputBinding). */
  readonly id = input.required<string>();
  readonly versaoId = input.required<string>();

  private readonly permissao = inject(PermissaoService);
  protected readonly podeEditar = this.permissao.temAlguma(['projetos.editar']);
  /** O cálculo do preço (custos, markup, cotações); sem a regra, o card mostra só o preço e o status. */
  protected readonly podeVerCalculo = this.permissao.temAlguma(['precificacao.ler']);
  protected readonly podeEditarOrcamento = this.permissao.temAlguma(['precificacao.editar']);
  protected readonly status = STATUS_VERSAO;
  protected readonly statusPeca = STATUS_PECA_DO_PROJETO;
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

  /**
   * Relido também quando só o `:versaoId` muda: ao duplicar, o router reaproveita esta página e a cópia ainda não está
   * no projeto que já foi lido.
   */
  protected readonly projeto = rxResource({
    params: () => ({ id: Number(this.id()), versaoId: Number(this.versaoId()) }),
    stream: ({ params }) => this.projetosService.getProjeto(params.id),
  });

  protected readonly versao = computed<VersaoProjetoDto | undefined>(() =>
    this.projeto.value()?.versoes.find((v) => v.id === Number(this.versaoId())),
  );
  protected readonly voltarPara = computed(() => `/projetos/${this.id()}`);
  protected readonly editavel = computed(() => this.podeEditar && this.versao()?.status === 'EM_DESENVOLVIMENTO');

  /**
   * O peso que vale para cada peça da composição: com modelo 3D, o medido nele (na liga da peça); sem 3D, o digitado.
   * Pedido à parte, porque medir os modelos leva um instante; enquanto não chega, vale o digitado.
   */
  protected readonly pesos = rxResource({
    params: () => ({ projetoId: Number(this.id()), versaoId: Number(this.versaoId()), itens: this.versao()?.itens.length ?? 0 }),
    stream: ({ params }) => this.projetosService.getPesosDaVersao(params.projetoId, params.versaoId),
  });

  /** Soma do peso das peças que têm peso (quantidade × peso da peça, o do modelo 3D quando ela tem). */
  protected readonly pesoTotal = computed(() => {
    const pesos = this.pesos.value();
    return (this.versao()?.itens ?? []).reduce((total, item) => total + item.quantidade * (pesos?.get(item.peca.id) ?? item.peca.pesoGramas ?? 0), 0);
  });
  protected readonly unidades = computed(() => (this.versao()?.itens ?? []).reduce((total, item) => total + item.quantidade, 0));

  /** Só as peças deste projeto entram na composição; a busca filtra por nome ou material, aqui mesmo. */
  protected readonly buscaPeca = new FormControl('', { nonNullable: true });
  private readonly termoPeca = toSignal(this.buscaPeca.valueChanges.pipe(debounceTime(150), distinctUntilChanged()), { initialValue: '' });
  protected readonly pecasDoProjeto = computed(() => {
    const termo = this.termoPeca().trim().toLocaleLowerCase('pt-BR');
    return (this.projeto.value()?.pecas ?? [])
      .filter((p) => p.ativo)
      .filter((p) => !termo || p.nome.toLocaleLowerCase('pt-BR').includes(termo) || (p.material?.toLocaleLowerCase('pt-BR').includes(termo) ?? false));
  });

  /** Peças em rascunho na composição: a publicação é recusada enquanto houver alguma. */
  protected readonly rascunhos = computed(() => {
    const versao = this.versao();
    return versao ? pecasEmRascunho(versao) : [];
  });
  /** Mostra o aviso das peças em rascunho depois de tocar em Publicar (some ao trocar de versão). */
  protected readonly publicacaoBloqueada = linkedSignal({ source: this.versaoId, computation: () => false });

  /** Quantidade escolhida para cada peça do projeto antes de tocar em Adicionar (volta a 1 ao trocar de versão). */
  private readonly quantidadesNovas = linkedSignal<string, Readonly<Record<number, number>>>({ source: this.versaoId, computation: () => ({}) });

  protected quantidadeNova(peca: PecaDoProjetoResumoDto): number {
    return this.quantidadesNovas()[peca.id] ?? 1;
  }

  protected definirQuantidadeNova(peca: PecaDoProjetoResumoDto, quantidade: number): void {
    this.quantidadesNovas.update((atual) => ({ ...atual, [peca.id]: quantidade }));
  }

  protected adicionar(peca: PecaDoProjetoResumoDto): void {
    const versao = this.versao();
    if (!versao) return;
    const quantidade = this.quantidadeNova(peca);
    this.executar(this.projetosService.postItem(this.projetoId(), versao.id, { pecaDoProjetoId: peca.id, quantidade }), `${quantidade}× ${peca.nome} adicionada.`, () =>
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
    if (!versao) return;
    // O servidor também recusa; aqui o aviso mostra as peças com atalho, sem ir ao servidor.
    if (this.rascunhos().length > 0) {
      this.publicacaoBloqueada.set(true);
      return;
    }
    this.publicacaoBloqueada.set(false);
    this.executar(this.projetosService.postPublicarVersao(this.projetoId(), versao.id), `Versão ${versao.numero} publicada.`);
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

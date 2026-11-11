import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { PermissaoService } from '@core/services/permissao.service';
import type { VersaoDoArquivoDto } from '@data/assistente/dto/ferramenta.dto';
import { type ArquivoFerramenta, type EscopoDaFerramenta, FerramentasService } from '@data/assistente/ferramentas.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { nomeDeArquivo, nomeDeArquivoDaPeca } from '@shared/arquivos/nome-de-arquivo';
import { DownloadService } from '@shared/services/download.service';
import { ToastService } from '@shared/services/toast.service';
import { AvisoPecaPublicadaComponent } from '@shared/ui/ferramenta-ia/aviso-peca-publicada.component';
import { firstValueFrom, map, of } from 'rxjs';

import { ConversaIaComponent } from '../../assistente/conversa-ia.component';

import { HistoricoDesenhoComponent } from './historico-desenho.component';
import { PonteCadCraft } from './ponte-cadcraft';

const FERRAMENTA = 'desenho-2d';

/**
 * Desenho 2D: o CADCraft completo na área de trabalho e a IA desta ferramenta no chat. Os dois trabalham
 * no mesmo arquivo, o DXF da versão (ou da peça do projeto, pela rota `pecas/:pecaId/desenho-2d`) guardado no
 * Castwork: a IA desenha nele pelo MCP (no servidor) e a tela troca o desenho; o que o joalheiro desenha à mão é salvo
 * antes de cada mensagem à IA. Na peça: uma conversa só, o joalheiro marca no histórico (ou no topo) qual arquivo é o
 * **final** (o que vale para a publicação e a ficha técnica), a tela avisa quando o arquivo à vista não é o final, e numa
 * peça publicada trocar o final faz dela outra peça: a tela avisa e confirma antes.
 *
 * A conversa é a padrão das ferramentas (`cw-conversa-ia`); aqui fica só o que é do desenho. Enquanto a
 * IA trabalha, o CADCraft continua livre. O desenho novo é aberto num segundo CADCraft, por trás, e só aparece pronto: a tela não
 * pisca nem mostra o carregamento.
 */
@Component({
  selector: 'cw-desenho-2d',
  imports: [ConversaIaComponent, HistoricoDesenhoComponent, AvisoPecaPublicadaComponent],
  templateUrl: './desenho-2d.component.html',
  styleUrl: './desenho-2d.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Desenho2dComponent {
  private readonly ferramentas = inject(FerramentasService);
  private readonly toast = inject(ToastService);
  private readonly download = inject(DownloadService);
  private readonly projetos = inject(ProjetosService);
  private readonly permissao = inject(PermissaoService);

  /** `:id` do projeto e `:versaoId` da versão ou `:pecaId` da peça do projeto (withComponentInputBinding). */
  readonly id = input.required<string>();
  readonly versaoId = input<string>();
  readonly pecaId = input<string>();

  protected readonly daPeca = computed(() => !!this.pecaId());
  protected readonly voltarPara = computed(() =>
    this.daPeca() ? `/projetos/${this.id()}/pecas/${this.pecaId()}` : `/projetos/${this.id()}/versoes/${this.versaoId()}`,
  );
  protected readonly projeto = computed(() => Number(this.id()));
  protected readonly escopo = computed<EscopoDaFerramenta>(() => (this.daPeca() ? { pecaId: Number(this.pecaId()) } : Number(this.versaoId())));
  /** A peça do projeto (só na rota da peça), para saber se está publicada. */
  protected readonly peca = rxResource({
    params: () => (this.daPeca() ? { projetoId: Number(this.id()), pecaId: Number(this.pecaId()) } : undefined),
    stream: ({ params }) =>
      params ? this.projetos.getPecasDoProjeto(params.projetoId, false).pipe(map((pecas) => pecas?.find((p) => p.id === params.pecaId) ?? null)) : of(null),
  });
  protected readonly publicada = computed(() => this.peca.value()?.status === 'PUBLICADA');
  protected readonly podeEditar = computed(() => this.permissao.temAlguma(['projetos.editar']));
  /** "Elo oval · Desenho 2D" na peça; na versão, só "Desenho 2D". */
  protected readonly titulo = computed(() => (this.daPeca() ? `${this.peca.value()?.nome ?? 'Peça'} · Desenho 2D` : 'Desenho 2D'));
  /** Os arquivos finais da peça (só na rota da peça). */
  protected readonly finais = rxResource({
    params: () => (this.daPeca() ? { projetoId: Number(this.id()), pecaId: Number(this.pecaId()) } : undefined),
    stream: ({ params }) => (params ? this.projetos.getArquivosFinaisDaPeca(params.projetoId, params.pecaId) : of([])),
  });
  protected readonly finalId = computed(() => this.finais.value()?.find((f) => f.ferramenta === FERRAMENTA)?.arquivoId ?? null);
  /** O arquivo à vista (o atual); nulo sem desenho. */
  protected readonly atualId = signal<number | null>(null);
  /** Na peça, o arquivo à vista não é o final: a publicação e a ficha técnica usam o final. */
  protected readonly atualNaoEFinal = computed(() => this.daPeca() && !this.finais.isLoading() && this.atualId() !== null && this.atualId() !== this.finalId());
  /** Recarregando o desenho no CADCraft (abrir, salvar, trazer o que a IA fez). */
  protected readonly sincronizando = signal<string | null>('Abrindo o CADCraft...');
  /** Versões do arquivo no painel de histórico; nulo com o painel fechado. */
  protected readonly historico = signal<readonly VersaoDoArquivoDto[] | null>(null);
  /** Qual dos dois CADCrafts está à vista (o outro prepara o próximo desenho por trás). */
  protected readonly ativo = signal<0 | 1>(0);
  /** Trocando o desenho por trás (aviso discreto, sem bloquear). */
  protected readonly trocando = signal(false);

  private readonly cadcraftA = viewChild.required<ElementRef<HTMLIFrameElement>>('cadcraftA');
  private readonly cadcraftB = viewChild.required<ElementRef<HTMLIFrameElement>>('cadcraftB');
  private readonly conversa = viewChild.required(ConversaIaComponent);
  private readonly avisoPublicada = viewChild.required(AvisoPecaPublicadaComponent);
  private ponte: PonteCadCraft | null = null;

  /** A IA está trabalhando: o CADCraft avisa e o que conflita (salvar, restaurar) espera. */
  protected readonly pensando = computed(() => this.conversa().pensando());

  /**
   * Antes de cada mensagem à IA: o que foi desenhado à mão vai para o Castwork (a IA trabalha no arquivo guardado). O que a
   * IA fizer não muda o arquivo final da peça.
   */
  protected readonly antesDeEnviar = async (): Promise<void> => {
    if (!this.ponte?.temAlteracoes()) return;
    this.sincronizando.set('Guardando o seu desenho...');
    try {
      await this.guardar(await this.ponte.salvar());
    } finally {
      this.sincronizando.set(null);
    }
  };

  protected async iniciar(): Promise<void> {
    if (this.ponte) return;
    this.ponte = this.criarPonte(0);
    const arquivo = await firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.escopo(), FERRAMENTA));
    this.atualId.set(arquivo?.id ?? null);
    await this.abrir(arquivo, this.daPeca() ? 'Abrindo o desenho da peça...' : 'Abrindo o desenho da versão...');
  }

  /** A IA mudou (ou desfez) o desenho: traz a versão nova sem recarregar à vista. */
  protected arquivoMudou(): void {
    void this.trazerArquivo();
  }

  /** Salvar não muda o arquivo final da peça: vira mais uma versão no histórico. */
  protected async salvar(): Promise<void> {
    if (!this.ponte) return;
    try {
      this.sincronizando.set('Guardando o desenho...');
      await this.guardar(await this.ponte.salvar(), 'Desenho salvo no Castwork.');
    } catch {
      this.toast.erro('Não foi possível salvar o desenho.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  protected async abrirHistorico(): Promise<void> {
    this.historico.set(await firstValueFrom(this.ferramentas.getHistorico(this.projetoId, this.escopo(), FERRAMENTA)));
  }

  /** Antes de trocar o desenho, guarda o que foi feito à mão: vira mais uma versão no histórico. */
  protected async restaurar(versao: VersaoDoArquivoDto): Promise<void> {
    try {
      if (this.ponte?.temAlteracoes()) {
        this.sincronizando.set('Guardando o seu desenho...');
        await this.guardar(await this.ponte.salvar());
      }

      const arquivoId = await firstValueFrom(this.ferramentas.postRestaurar(this.projetoId, this.escopo(), FERRAMENTA, versao.id));
      this.conversa().registrarArquivo(arquivoId);
      this.historico.set(null);
      // Solta a prancheta antes da troca: o desenho novo abre por trás, só com o aviso discreto.
      this.sincronizando.set(null);
      await this.trazerArquivo();
      this.toast.sucesso('Desenho restaurado.');
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível restaurar o desenho.');
      this.sincronizando.set(null);
    }
  }

  /**
   * Marca como final da peça o arquivo do histórico (`arquivoId`) ou, sem ele, o à vista (o que foi desenhado à mão é salvo
   * antes). Numa peça publicada, trocar o final faz dela outra peça: confirma antes.
   */
  protected async marcarComoFinal(arquivoId?: number): Promise<void> {
    if (!this.daPeca() || this.sincronizando() || this.pensando() || !(await this.avisoPublicada().confirmar())) return;
    try {
      if (arquivoId === undefined && this.ponte?.temAlteracoes()) {
        this.sincronizando.set('Guardando o seu desenho...');
        await this.guardar(await this.ponte.salvar());
      }
      const id = arquivoId ?? this.atualId();
      if (id === null) {
        this.toast.erro('Salve o desenho antes de marcá-lo como final.');
        return;
      }
      this.sincronizando.set('Marcando como final...');
      const final = await firstValueFrom(this.ferramentas.postMarcarFinal(this.projetoId, this.escopo(), FERRAMENTA, id));
      this.historico.set(null);
      this.finais.reload();
      this.peca.reload();
      this.toast.sucesso(final.virouOutraPeca ? 'Arquivo final marcado. A peça voltou a rascunho como outra peça.' : 'Arquivo final marcado.');
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível marcar o arquivo final.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  /** Na peça, o arquivo baixado leva o nome dela ("Elo oval.dxf"). */
  protected async baixar(versao: VersaoDoArquivoDto): Promise<void> {
    const arquivo = await firstValueFrom(this.ferramentas.getVersaoDoArquivo(this.projetoId, this.escopo(), FERRAMENTA, versao.id));
    const nome = this.daPeca() ? nomeDeArquivoDaPeca(this.peca.value()?.nome, 'dxf') : `desenho-${versao.id}.dxf`;
    if (arquivo) this.download.salvar(arquivo, nome);
  }

  private get projetoId(): number {
    return Number(this.id());
  }

  private async guardar(arquivo: Blob, aviso?: string): Promise<void> {
    const arquivoId = await firstValueFrom(this.ferramentas.putArquivo(this.projetoId, this.escopo(), FERRAMENTA, await emBase64(arquivo)));
    this.conversa().registrarArquivo(arquivoId);
    this.atualId.set(arquivoId);
    if (aviso) this.toast.sucesso(aviso);
  }

  /**
   * Traz o arquivo atual sem recarregar à vista: o outro CADCraft abre o desenho por trás e, pronto, troca
   * de lugar com o que estava na tela, que é liberado.
   */
  private async trazerArquivo(): Promise<void> {
    const arquivo = await firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.escopo(), FERRAMENTA));
    this.atualId.set(arquivo?.id ?? null);
    const atras = this.ativo() === 0 ? 1 : 0;
    const nova = this.criarPonte(atras);
    this.trocando.set(true);
    try {
      await nova.carregar(await this.conteudoOuVazio(arquivo), nomeDeArquivo(arquivo?.nome, 'dxf', 'desenho'));
      const antiga = this.ponte;
      this.ponte = nova;
      this.ativo.set(atras);
      antiga?.descarregar();
    } catch {
      nova.descarregar();
      this.toast.erro('Não foi possível abrir o desenho no CADCraft.');
    } finally {
      this.trocando.set(false);
    }
  }

  private criarPonte(indice: 0 | 1): PonteCadCraft {
    const iframe = indice === 0 ? this.cadcraftA() : this.cadcraftB();
    // O ⌘S do joalheiro dentro do CADCraft também salva no Castwork (sem mudar o arquivo final da peça).
    return new PonteCadCraft(iframe.nativeElement, (arquivo) => void this.guardar(arquivo, 'Desenho salvo no Castwork.'));
  }

  /** Arquivo estático do próprio front (fora da API): fetch direto, sem os interceptors. */
  private async conteudoOuVazio(arquivo: ArquivoFerramenta | null): Promise<Blob> {
    return arquivo?.conteudo ?? (await (await fetch('/desenho-vazio.dxf')).blob());
  }

  /**
   * Sem arquivo na versão, abre um DXF vazio em milímetros com nome: um desenho "Drawing1" sem nome
   * faria o ⌘S abrir o Salvar como, e o Castwork não receberia o arquivo.
   */
  private async abrir(arquivo: ArquivoFerramenta | null, mensagem: string): Promise<void> {
    this.sincronizando.set(mensagem);
    try {
      const desenho = await this.conteudoOuVazio(arquivo);
      // A aba do CADCraft leva o nome que o joalheiro deu ao arquivo (sem acento e sem espaço).
      await this.ponte?.carregar(desenho, nomeDeArquivo(arquivo?.nome, 'dxf', 'desenho'));
    } catch {
      this.toast.erro('Não foi possível abrir o desenho no CADCraft.');
    } finally {
      this.sincronizando.set(null);
    }
  }
}

async function emBase64(arquivo: Blob): Promise<string> {
  const bytes = new Uint8Array(await arquivo.arrayBuffer());
  let binario = '';
  for (let i = 0; i < bytes.length; i += 0x8000) {
    binario += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
  }
  return btoa(binario);
}

import { valorDe } from '@shared/recursos/valor-de';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, afterNextRender, computed, inject, input, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import type { VersaoDoArquivoDto } from '@data/assistente/dto/ferramenta.dto';
import { PermissaoService } from '@core/services/permissao.service';
import { type EscopoDaFerramenta, FerramentasService } from '@data/assistente/ferramentas.service';
import { Modelo3dService } from '@data/ferramentas/modelo-3d/modelo-3d.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { nomeDeArquivo, nomeDeArquivoDaPeca } from '@shared/arquivos/nome-de-arquivo';
import { DownloadService } from '@shared/services/download.service';
import { ToastService } from '@shared/services/toast.service';
import { AvisoPecaPublicadaComponent } from '@shared/ui/ferramenta-ia/aviso-peca-publicada.component';
import { firstValueFrom, map, of } from 'rxjs';

import { ConversaIaComponent } from '../../assistente/conversa-ia.component';

import { ConferenciaModeloComponent } from './conferencia-modelo.component';
import { avisoDeCorposSemMaterial } from './corpos-sem-material';
import { HistoricoModeloComponent } from './historico-modelo.component';
import { resumoDaMontagem } from './montagem';
import { PonteSolidCraft } from './ponte-solidcraft';

const FERRAMENTA = 'modelo-3d';

/**
 * Modelo 3D: o SolidCraft completo na área de trabalho e a IA desta ferramenta no chat. Os dois trabalham
 * no mesmo arquivo, o projeto 3D (JSON) da versão (ou da peça do projeto, pela rota `pecas/:pecaId/modelo-3d`)
 * guardado no Castwork: a IA modela nele pelo MCP (no servidor) e a tela troca o projeto; o que o joalheiro modela à
 * mão é salvo antes de cada mensagem à IA.
 *
 * Como no desenho 2D, o projeto novo é aberto num segundo SolidCraft, por trás, e só aparece pronto. Na versão: a
 * conferência de peso e materiais e o "Montar a partir das peças" (uma cópia do modelo de cada peça da composição por
 * unidade). Na peça: uma conversa só, o joalheiro marca no histórico (ou no topo) qual arquivo é o **final** (o que vale
 * para o peso, a montagem e a publicação; ao marcar, os corpos recebem o nome da peça), a tela avisa quando o arquivo à
 * vista não é o final, e numa peça publicada trocar o final faz dela outra peça: a tela avisa e confirma antes.
 */
@Component({
  selector: 'cw-modelo-3d',
  imports: [ConversaIaComponent, ConferenciaModeloComponent, HistoricoModeloComponent, AvisoPecaPublicadaComponent],
  templateUrl: './modelo-3d.component.html',
  styleUrl: './modelo-3d.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Modelo3dComponent {
  private readonly ferramentas = inject(FerramentasService);
  private readonly toast = inject(ToastService);
  private readonly download = inject(DownloadService);
  private readonly permissao = inject(PermissaoService);
  private readonly projetos = inject(ProjetosService);
  private readonly modelo3d = inject(Modelo3dService);

  /** `:id` do projeto e `:versaoId` da versão ou `:pecaId` da peça do projeto (withComponentInputBinding). */
  readonly id = input.required<string>();
  readonly versaoId = input<string>();
  readonly pecaId = input<string>();
  /** `?conferir=1` (do card Preço): abre a conferência de peso e materiais assim que o modelo estiver aberto. */
  readonly conferir = input<string>();

  protected readonly daPeca = computed(() => !!this.pecaId());
  protected readonly voltarPara = computed(() =>
    this.daPeca() ? `/projetos/${this.id()}/pecas/${this.pecaId()}` : `/projetos/${this.id()}/versoes/${this.versaoId()}`,
  );
  protected readonly projeto = computed(() => Number(this.id()));
  protected readonly versao = computed(() => Number(this.versaoId()));
  protected readonly escopo = computed<EscopoDaFerramenta>(() => (this.daPeca() ? { pecaId: Number(this.pecaId()) } : Number(this.versaoId())));
  /** A peça do projeto (só na rota da peça), para saber se está publicada. */
  protected readonly peca = rxResource({
    params: () => (this.daPeca() ? { projetoId: Number(this.id()), pecaId: Number(this.pecaId()) } : undefined),
    stream: ({ params }) =>
      params ? this.projetos.getPecasDoProjeto(params.projetoId, false).pipe(map((pecas) => pecas?.find((p) => p.id === params.pecaId) ?? null)) : of(null),
  });
  protected readonly publicada = computed(() => valorDe(this.peca)?.status === 'PUBLICADA');
  /** "Elo oval · Modelo 3D" na peça; na versão, só "Modelo 3D". */
  protected readonly titulo = computed(() => (this.daPeca() ? `${valorDe(this.peca)?.nome ?? 'Peça'} · Modelo 3D` : 'Modelo 3D'));
  /** Os arquivos finais da peça (só na rota da peça). */
  protected readonly finais = rxResource({
    params: () => (this.daPeca() ? { projetoId: Number(this.id()), pecaId: Number(this.pecaId()) } : undefined),
    stream: ({ params }) => (params ? this.projetos.getArquivosFinaisDaPeca(params.projetoId, params.pecaId) : of([])),
  });
  protected readonly finalId = computed(() => valorDe(this.finais)?.find((f) => f.ferramenta === FERRAMENTA)?.arquivoId ?? null);
  /** O arquivo à vista (o atual); nulo sem modelo. */
  protected readonly atualId = signal<number | null>(null);
  /** Na peça, o arquivo à vista não é o final: o peso, a montagem e a publicação usam o final. */
  protected readonly atualNaoEFinal = computed(() => this.daPeca() && !this.finais.isLoading() && this.atualId() !== null && this.atualId() !== this.finalId());
  /** Recarregando o projeto no SolidCraft (abrir, salvar, trazer o que a IA fez). */
  protected readonly sincronizando = signal<string | null>('Abrindo o SolidCraft...');
  /** Versões do arquivo no painel de histórico; nulo com o painel fechado. */
  protected readonly historico = signal<readonly VersaoDoArquivoDto[] | null>(null);
  /** Qual dos dois SolidCrafts está à vista (o outro prepara o próximo projeto por trás). */
  protected readonly ativo = signal<0 | 1>(0);
  /** Trocando o projeto por trás (aviso discreto; o que grava ou troca o arquivo espera). */
  protected readonly trocando = signal(false);
  /**
   * O modelo não abriu, ou a troca falhou: o que está à vista não é o arquivo atual. Nada é gravado até abrir de novo
   * (salvar agora poria um projeto em branco, ou o antigo, por cima do atual).
   */
  protected readonly falha = signal(false);
  /** Corpos do modelo sem material (o "Default" do SolidCraft): aviso discreto, sem bloquear a modelagem. */
  protected readonly semMaterial = signal<readonly string[]>([]);
  protected readonly avisoSemMaterial = computed(() => avisoDeCorposSemMaterial(this.semMaterial().length));
  /** A conferência de peso e materiais aberta por cima do modelo. */
  protected readonly conferindo = signal(false);
  protected readonly podeEditar = computed(() => this.permissao.temAlguma(['projetos.editar']));

  private readonly solidcraftA = viewChild.required<ElementRef<HTMLIFrameElement>>('solidcraftA');
  private readonly solidcraftB = viewChild.required<ElementRef<HTMLIFrameElement>>('solidcraftB');
  private readonly conversa = viewChild.required(ConversaIaComponent);
  private readonly avisoPublicada = viewChild.required(AvisoPecaPublicadaComponent);
  private ponte: PonteSolidCraft | null = null;

  /** A IA está trabalhando: o SolidCraft avisa e o que conflita (salvar, restaurar) espera. */
  protected readonly pensando = computed(() => this.conversa().pensando());
  /** Nada que grave ou troque o arquivo começa enquanto outra operação, a IA ou uma troca estiver em andamento. */
  protected readonly ocupado = computed(() => !!this.sincronizando() || this.pensando() || this.trocando() || this.falha());

  /** A primeira abertura: as trocas esperam por ela, para não descarregar o SolidCraft que ainda está subindo. */
  private aberturaConcluida!: () => void;
  private readonly abertura = new Promise<void>((concluida) => (this.aberturaConcluida = concluida));
  /** A troca em andamento e se chegou outro pedido durante ela (uma troca por vez; ver `trazerArquivo`). */
  private troca: Promise<void> | null = null;
  private trocaPendente = false;

  /**
   * Antes de cada mensagem à IA: o que foi modelado à mão vai para o Castwork (a IA trabalha no arquivo guardado). O que a
   * IA fizer não muda o arquivo final da peça.
   */
  protected readonly antesDeEnviar = async (): Promise<void> => {
    // Durante a troca, o SolidCraft à vista ainda é o antigo: guardá-lo agora apagaria o que acabou de chegar.
    await this.troca;
    if (this.falha() || !(await this.ponte?.temAlteracoes())) return;
    this.sincronizando.set('Guardando o seu modelo...');
    try {
      await this.ponte?.salvar((arquivo) => this.guardar(arquivo));
    } finally {
      this.sincronizando.set(null);
    }
  };

  constructor() {
    afterNextRender(() => void this.iniciar());
  }

  private async iniciar(): Promise<void> {
    if (this.ponte) return;
    this.ponte = this.criarPonte(0);
    this.sincronizando.set(this.daPeca() ? 'Abrindo o modelo da peça...' : 'Abrindo o modelo da versão...');
    try {
      const arquivo = await firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.escopo(), FERRAMENTA));
      await this.ponte.carregar(arquivo?.conteudo ?? null, nomeDeArquivo(arquivo?.nome, 'json', 'modelo').replace(/\.json$/, ''));
      this.atualId.set(arquivo?.id ?? null);
      void this.conferirMateriais();
    } catch {
      this.falha.set(true);
      this.toast.erro('Não foi possível abrir o modelo no SolidCraft.');
    } finally {
      this.sincronizando.set(null);
      this.aberturaConcluida();
    }

    if (this.conferir() && !this.daPeca()) void this.abrirConferencia();
  }

  /**
   * Abre a conferência de peso e materiais. O que foi modelado à mão vai antes para o Castwork: a conferência mede e
   * altera o arquivo guardado.
   */
  protected async abrirConferencia(): Promise<void> {
    if (this.ocupado()) return;
    try {
      if (await this.ponte?.temAlteracoes()) {
        this.sincronizando.set('Guardando o seu modelo...');
        await this.ponte?.salvar((arquivo) => this.guardar(arquivo));
      }
      this.conferindo.set(true);
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível guardar o modelo antes de conferir.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  /** A conferência gravou um arquivo novo (material, união ou desfazer): o SolidCraft à vista recarrega. */
  protected modeloConferido(arquivoId: number): void {
    this.conversa().registrarArquivo(arquivoId);
    void this.trazerArquivo();
  }

  /** A IA mudou (ou desfez) o modelo: traz a versão nova sem recarregar à vista. */
  protected arquivoMudou(): void {
    void this.trazerArquivo();
  }

  /**
   * Traz para o modelo da versão uma cópia do modelo de cada peça da composição, uma por unidade, enfileiradas para
   * posicionar depois. O que foi modelado à mão é salvo antes (a montagem parte do arquivo guardado).
   */
  protected async montar(): Promise<void> {
    if (this.daPeca() || this.ocupado()) return;
    try {
      if (await this.ponte?.temAlteracoes()) {
        this.sincronizando.set('Guardando o seu modelo...');
        await this.ponte?.salvar((arquivo) => this.guardar(arquivo));
      }
      this.sincronizando.set('Trazendo as peças para o modelo...');
      const montado = await firstValueFrom(this.modelo3d.postMontarAPartirDasPecas(this.projetoId, this.versao()));
      this.conversa().registrarArquivo(montado.arquivoId);
      this.sincronizando.set(null);
      await this.trazerArquivo();
      this.toast.sucesso(resumoDaMontagem(montado));
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível montar o modelo a partir das peças.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  /** Salvar não muda o arquivo final da peça: vira mais uma versão no histórico. */
  protected async salvar(): Promise<void> {
    if (!this.ponte || this.ocupado()) return;
    try {
      this.sincronizando.set('Guardando o modelo...');
      await this.ponte.salvar((arquivo) => this.guardar(arquivo, 'Modelo salvo no Castwork.'));
      void this.conferirMateriais();
    } catch (erro) {
      // Os erros da API já viram toast no interceptor.
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível salvar o modelo.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  protected async abrirHistorico(): Promise<void> {
    try {
      this.historico.set(await firstValueFrom(this.ferramentas.getHistorico(this.projetoId, this.escopo(), FERRAMENTA)));
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível abrir o histórico.');
    }
  }

  /** Antes de trocar o projeto, guarda o que foi feito à mão: vira mais uma versão no histórico. */
  protected async restaurar(versao: VersaoDoArquivoDto): Promise<void> {
    if (this.ocupado()) return;
    this.sincronizando.set('Restaurando o modelo...');
    try {
      if (await this.ponte?.temAlteracoes()) {
        this.sincronizando.set('Guardando o seu modelo...');
        await this.ponte?.salvar((arquivo) => this.guardar(arquivo));
      }

      const arquivoId = await firstValueFrom(this.ferramentas.postRestaurar(this.projetoId, this.escopo(), FERRAMENTA, versao.id));
      this.conversa().registrarArquivo(arquivoId);
      this.historico.set(null);
      this.sincronizando.set(null);
      await this.trazerArquivo();
      this.toast.sucesso('Modelo restaurado.');
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível restaurar o modelo.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  /**
   * Marca como final da peça o arquivo do histórico (`arquivoId`) ou, sem ele, o à vista (o que foi modelado à mão é salvo
   * antes). Numa peça publicada, trocar o final faz dela outra peça: confirma antes. Os corpos recebem o nome da peça numa
   * versão nova do arquivo, que o SolidCraft à vista recarrega.
   */
  protected async marcarComoFinal(arquivoId?: number): Promise<void> {
    if (!this.daPeca() || this.ocupado() || !(await this.avisoPublicada().confirmar())) return;
    try {
      if (arquivoId === undefined && (await this.ponte?.temAlteracoes())) {
        this.sincronizando.set('Guardando o seu modelo...');
        await this.ponte?.salvar((arquivo) => this.guardar(arquivo));
      }
      const id = arquivoId ?? this.atualId();
      if (id === null) {
        this.toast.erro('Salve o modelo antes de marcá-lo como final.');
        return;
      }
      this.sincronizando.set('Marcando como final...');
      const final = await firstValueFrom(this.ferramentas.postMarcarFinal(this.projetoId, this.escopo(), FERRAMENTA, id));
      this.historico.set(null);
      this.finais.reload();
      this.peca.reload();
      if (final.ajustado) {
        this.conversa().registrarArquivo(final.arquivoId);
        this.sincronizando.set(null);
        await this.trazerArquivo();
      }
      this.toast.sucesso(final.virouOutraPeca ? 'Arquivo final marcado. A peça voltou a rascunho como outra peça.' : 'Arquivo final marcado.');
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível marcar o arquivo final.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  /** Na peça, o arquivo baixado leva o nome dela ("Elo oval.json"). */
  protected async baixar(versao: VersaoDoArquivoDto): Promise<void> {
    try {
      const arquivo = await firstValueFrom(this.ferramentas.getVersaoDoArquivo(this.projetoId, this.escopo(), FERRAMENTA, versao.id));
      const nome = this.daPeca() ? nomeDeArquivoDaPeca(valorDe(this.peca)?.nome, 'json') : `modelo-${versao.id}.json`;
      if (arquivo) this.download.salvar(arquivo, nome);
      else this.toast.erro('Esta versão está em branco: não há arquivo para baixar.');
    } catch {
      // O arquivo vem como Blob: o interceptor não avisa.
      this.toast.erro('Não foi possível baixar o modelo.');
    }
  }

  /** Depois de uma falha ao abrir ou trocar: abre de novo o arquivo atual. */
  protected tentarDeNovo(): void {
    void this.trazerArquivo();
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
   * Traz o arquivo atual sem recarregar à vista: o outro SolidCraft abre o projeto por trás e, pronto, troca
   * de lugar com o que estava na tela, que é liberado.
   *
   * Uma troca por vez: um pedido que chega durante outra não abre uma segunda (as duas disputariam o mesmo iframe e
   * acabariam com os dois vazios); a troca em andamento roda mais uma vez ao terminar, já com o arquivo mais novo.
   * Nunca rejeita: a falha vira aviso na tela (`falha`).
   */
  private trazerArquivo(): Promise<void> {
    if (this.troca) {
      this.trocaPendente = true;
      return this.troca;
    }
    this.trocando.set(true);
    this.troca = (async () => {
      try {
        await this.abertura;
        do {
          this.trocaPendente = false;
          await this.trocar();
        } while (this.trocaPendente);
      } finally {
        this.troca = null;
        this.trocando.set(false);
      }
    })();
    return this.troca;
  }

  private async trocar(): Promise<void> {
    const atras = this.ativo() === 0 ? 1 : 0;
    const nova = this.criarPonte(atras);
    try {
      const arquivo = await firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.escopo(), FERRAMENTA));
      await nova.carregar(arquivo?.conteudo ?? null, nomeDeArquivo(arquivo?.nome, 'json', 'modelo').replace(/\.json$/, ''));
      const antiga = this.ponte;
      this.ponte = nova;
      this.ativo.set(atras);
      antiga?.descarregar();
      // Só agora o arquivo novo é o que está à vista.
      this.atualId.set(arquivo?.id ?? null);
      this.falha.set(false);
      void this.conferirMateriais();
    } catch {
      nova.descarregar();
      this.falha.set(true);
      this.toast.erro('Não foi possível abrir o modelo no SolidCraft.');
    }
  }

  /**
   * Confere os materiais do modelo à vista; se o SolidCraft não responder, só não mostra o aviso. Na peça não confere: o
   * peso dela é medido todo na liga da peça.
   */
  private async conferirMateriais(): Promise<void> {
    if (this.daPeca()) return;
    try {
      this.semMaterial.set((await this.ponte?.corposSemMaterial()) ?? []);
    } catch {
      this.semMaterial.set([]);
    }
  }

  private criarPonte(indice: 0 | 1): PonteSolidCraft {
    const iframe = indice === 0 ? this.solidcraftA() : this.solidcraftB();
    return new PonteSolidCraft(iframe.nativeElement, () => void this.salvar());
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

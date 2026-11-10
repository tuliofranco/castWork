import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, afterNextRender, computed, inject, input, signal, viewChild } from '@angular/core';
import type { VersaoDoArquivoDto } from '@data/assistente/dto/ferramenta.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { nomeDeArquivo } from '@shared/arquivos/nome-de-arquivo';
import { DownloadService } from '@shared/services/download.service';
import { ToastService } from '@shared/services/toast.service';
import { firstValueFrom } from 'rxjs';

import { ConversaIaComponent } from '../../assistente/conversa-ia.component';

import { avisoDeCorposSemMaterial } from './corpos-sem-material';
import { HistoricoModeloComponent } from './historico-modelo.component';
import { PonteSolidCraft } from './ponte-solidcraft';

const FERRAMENTA = 'modelo-3d';

/**
 * Modelo 3D: o SolidCraft completo na área de trabalho e a IA desta ferramenta no chat. Os dois trabalham
 * no mesmo arquivo, o projeto 3D (JSON) da versão guardado no Castwork: a IA modela nele pelo MCP (no
 * servidor) e a tela troca o projeto; o que o joalheiro modela à mão é salvo antes de cada mensagem à IA.
 *
 * Como no desenho 2D, o projeto novo é aberto num segundo SolidCraft, por trás, e só aparece pronto.
 */
@Component({
  selector: 'cw-modelo-3d',
  imports: [ConversaIaComponent, HistoricoModeloComponent],
  templateUrl: './modelo-3d.component.html',
  styleUrl: './modelo-3d.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Modelo3dComponent {
  private readonly ferramentas = inject(FerramentasService);
  private readonly toast = inject(ToastService);
  private readonly download = inject(DownloadService);

  readonly id = input.required<string>();
  readonly versaoId = input.required<string>();

  protected readonly voltarPara = computed(() => `/projetos/${this.id()}/versoes/${this.versaoId()}`);
  protected readonly projeto = computed(() => Number(this.id()));
  protected readonly versao = computed(() => Number(this.versaoId()));
  /** Recarregando o projeto no SolidCraft (abrir, salvar, trazer o que a IA fez). */
  protected readonly sincronizando = signal<string | null>('Abrindo o SolidCraft...');
  /** Versões do arquivo no painel de histórico; nulo com o painel fechado. */
  protected readonly historico = signal<readonly VersaoDoArquivoDto[] | null>(null);
  /** Qual dos dois SolidCrafts está à vista (o outro prepara o próximo projeto por trás). */
  protected readonly ativo = signal<0 | 1>(0);
  /** Trocando o projeto por trás (aviso discreto, sem bloquear). */
  protected readonly trocando = signal(false);
  /** Corpos do modelo sem material (o "Default" do SolidCraft): aviso discreto, sem bloquear a modelagem. */
  protected readonly semMaterial = signal<readonly string[]>([]);
  protected readonly avisoSemMaterial = computed(() => avisoDeCorposSemMaterial(this.semMaterial().length));

  private readonly solidcraftA = viewChild.required<ElementRef<HTMLIFrameElement>>('solidcraftA');
  private readonly solidcraftB = viewChild.required<ElementRef<HTMLIFrameElement>>('solidcraftB');
  private readonly conversa = viewChild.required(ConversaIaComponent);
  private ponte: PonteSolidCraft | null = null;

  /** A IA está trabalhando: o SolidCraft avisa e o que conflita (salvar, restaurar) espera. */
  protected readonly pensando = computed(() => this.conversa().pensando());

  /** Antes de cada mensagem à IA: o que foi modelado à mão vai para o Castwork (a IA trabalha no arquivo guardado). */
  protected readonly antesDeEnviar = async (): Promise<void> => {
    if (!(await this.ponte?.temAlteracoes())) return;
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
    this.sincronizando.set('Abrindo o modelo da versão...');
    try {
      const arquivo = await firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.numeroDaVersao, FERRAMENTA));
      await this.ponte.carregar(arquivo?.conteudo ?? null, nomeDeArquivo(arquivo?.nome, 'json', 'modelo').replace(/\.json$/, ''));
      void this.conferirMateriais();
    } catch {
      this.toast.erro('Não foi possível abrir o modelo no SolidCraft.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  /** A IA mudou (ou desfez) o modelo: traz a versão nova sem recarregar à vista. */
  protected arquivoMudou(): void {
    void this.trazerArquivo();
  }

  protected async salvar(): Promise<void> {
    if (!this.ponte || this.sincronizando() || this.pensando()) return;
    try {
      this.sincronizando.set('Guardando o modelo...');
      await this.ponte.salvar((arquivo) => this.guardar(arquivo, 'Modelo salvo no Castwork.'));
      void this.conferirMateriais();
    } catch {
      this.toast.erro('Não foi possível salvar o modelo.');
    } finally {
      this.sincronizando.set(null);
    }
  }

  protected async abrirHistorico(): Promise<void> {
    this.historico.set(await firstValueFrom(this.ferramentas.getHistorico(this.projetoId, this.numeroDaVersao, FERRAMENTA)));
  }

  /** Antes de trocar o projeto, guarda o que foi feito à mão: vira mais uma versão no histórico. */
  protected async restaurar(versao: VersaoDoArquivoDto): Promise<void> {
    try {
      if (await this.ponte?.temAlteracoes()) {
        this.sincronizando.set('Guardando o seu modelo...');
        await this.ponte?.salvar((arquivo) => this.guardar(arquivo));
      }

      const arquivoId = await firstValueFrom(this.ferramentas.postRestaurar(this.projetoId, this.numeroDaVersao, FERRAMENTA, versao.id));
      this.conversa().registrarArquivo(arquivoId);
      this.historico.set(null);
      this.sincronizando.set(null);
      await this.trazerArquivo();
      this.toast.sucesso('Modelo restaurado.');
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível restaurar o modelo.');
      this.sincronizando.set(null);
    }
  }

  protected async baixar(versao: VersaoDoArquivoDto): Promise<void> {
    const arquivo = await firstValueFrom(this.ferramentas.getVersaoDoArquivo(this.projetoId, this.numeroDaVersao, FERRAMENTA, versao.id));
    if (arquivo) this.download.salvar(arquivo, `modelo-${versao.id}.json`);
  }

  private get projetoId(): number {
    return Number(this.id());
  }

  private get numeroDaVersao(): number {
    return Number(this.versaoId());
  }

  private async guardar(arquivo: Blob, aviso?: string): Promise<void> {
    const arquivoId = await firstValueFrom(this.ferramentas.putArquivo(this.projetoId, this.numeroDaVersao, FERRAMENTA, await emBase64(arquivo)));
    this.conversa().registrarArquivo(arquivoId);
    if (aviso) this.toast.sucesso(aviso);
  }

  /**
   * Traz o arquivo atual sem recarregar à vista: o outro SolidCraft abre o projeto por trás e, pronto, troca
   * de lugar com o que estava na tela, que é liberado.
   */
  private async trazerArquivo(): Promise<void> {
    const arquivo = await firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.numeroDaVersao, FERRAMENTA));
    const atras = this.ativo() === 0 ? 1 : 0;
    const nova = this.criarPonte(atras);
    this.trocando.set(true);
    try {
      await nova.carregar(arquivo?.conteudo ?? null, nomeDeArquivo(arquivo?.nome, 'json', 'modelo').replace(/\.json$/, ''));
      const antiga = this.ponte;
      this.ponte = nova;
      this.ativo.set(atras);
      antiga?.descarregar();
      void this.conferirMateriais();
    } catch {
      nova.descarregar();
      this.toast.erro('Não foi possível abrir o modelo no SolidCraft.');
    } finally {
      this.trocando.set(false);
    }
  }

  /** Confere os materiais do modelo à vista; se o SolidCraft não responder, só não mostra o aviso. */
  private async conferirMateriais(): Promise<void> {
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

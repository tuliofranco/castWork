import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, signal, viewChild } from '@angular/core';
import type { VersaoDoArquivoDto } from '@data/projetos/ferramentas/dto/ferramenta.dto';
import { type ArquivoFerramenta, FerramentasService } from '@data/projetos/ferramentas/ferramentas.service';
import { nomeDeArquivo } from '@shared/arquivos/nome-de-arquivo';
import { DownloadService } from '@shared/services/download.service';
import { ToastService } from '@shared/services/toast.service';
import { firstValueFrom } from 'rxjs';

import { ConversaIaComponent } from '../ferramentas/conversa-ia.component';

import { HistoricoDesenhoComponent } from './historico-desenho.component';
import { PonteCadCraft } from './ponte-cadcraft';

const FERRAMENTA = 'desenho-2d';

/**
 * Desenho 2D: o CADCraft completo na área de trabalho e a IA desta ferramenta no chat. Os dois trabalham
 * no mesmo arquivo, o DXF da versão guardado no Castwork: a IA desenha nele pelo MCP (no servidor) e a
 * tela troca o desenho; o que o joalheiro desenha à mão é salvo antes de cada mensagem à IA.
 *
 * A conversa é a padrão das ferramentas (`cw-conversa-ia`); aqui fica só o que é do desenho. Enquanto a
 * IA trabalha, o CADCraft continua livre. O desenho novo é aberto num segundo CADCraft, por trás, e só aparece pronto: a tela não
 * pisca nem mostra o carregamento.
 */
@Component({
  selector: 'cw-desenho-2d',
  imports: [ConversaIaComponent, HistoricoDesenhoComponent],
  templateUrl: './desenho-2d.component.html',
  styleUrl: './desenho-2d.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Desenho2dComponent {
  private readonly ferramentas = inject(FerramentasService);
  private readonly toast = inject(ToastService);
  private readonly download = inject(DownloadService);

  readonly id = input.required<string>();
  readonly versaoId = input.required<string>();

  protected readonly voltarPara = computed(() => `/projetos/${this.id()}/versoes/${this.versaoId()}`);
  protected readonly projeto = computed(() => Number(this.id()));
  protected readonly versao = computed(() => Number(this.versaoId()));
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
  private ponte: PonteCadCraft | null = null;

  /** A IA está trabalhando: o CADCraft avisa e o que conflita (salvar, restaurar) espera. */
  protected readonly pensando = computed(() => this.conversa().pensando());

  /** Antes de cada mensagem à IA: o que foi desenhado à mão vai para o Castwork (a IA trabalha no arquivo guardado). */
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
    const arquivo = await firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.numeroDaVersao, FERRAMENTA));
    await this.abrir(arquivo, 'Abrindo o desenho da versão...');
  }

  /** A IA mudou (ou desfez) o desenho: traz a versão nova sem recarregar à vista. */
  protected arquivoMudou(): void {
    void this.trazerArquivo();
  }

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
    this.historico.set(await firstValueFrom(this.ferramentas.getHistorico(this.projetoId, this.numeroDaVersao, FERRAMENTA)));
  }

  /** Antes de trocar o desenho, guarda o que foi feito à mão: vira mais uma versão no histórico. */
  protected async restaurar(versao: VersaoDoArquivoDto): Promise<void> {
    try {
      if (this.ponte?.temAlteracoes()) {
        this.sincronizando.set('Guardando o seu desenho...');
        await this.guardar(await this.ponte.salvar());
      }

      const arquivoId = await firstValueFrom(this.ferramentas.postRestaurar(this.projetoId, this.numeroDaVersao, FERRAMENTA, versao.id));
      this.conversa().registrarArquivo(arquivoId);
      this.historico.set(null);
      await this.trazerArquivo();
      this.toast.sucesso('Desenho restaurado.');
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível restaurar o desenho.');
      this.sincronizando.set(null);
    }
  }

  protected async baixar(versao: VersaoDoArquivoDto): Promise<void> {
    const arquivo = await firstValueFrom(this.ferramentas.getVersaoDoArquivo(this.projetoId, this.numeroDaVersao, FERRAMENTA, versao.id));
    if (arquivo) this.download.salvar(arquivo, `desenho-${versao.id}.dxf`);
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
   * Traz o arquivo atual sem recarregar à vista: o outro CADCraft abre o desenho por trás e, pronto, troca
   * de lugar com o que estava na tela, que é liberado.
   */
  private async trazerArquivo(): Promise<void> {
    const arquivo = await firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.numeroDaVersao, FERRAMENTA));
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

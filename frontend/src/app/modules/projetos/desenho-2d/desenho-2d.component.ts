import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, signal, viewChild } from '@angular/core';
import type { ConversaFerramentaDto, VersaoDoArquivoDto } from '@data/projetos/ferramentas/dto/ferramenta.dto';
import { FerramentasService } from '@data/projetos/ferramentas/ferramentas.service';
import { DownloadService } from '@shared/services/download.service';
import { ToastService } from '@shared/services/toast.service';
import { type FalaFerramenta, FerramentaIaComponent, type MensagemFerramenta } from '@shared/ui/ferramenta-ia/ferramenta-ia.component';
import { firstValueFrom } from 'rxjs';

import { HistoricoDesenhoComponent } from './historico-desenho.component';
import { PonteCadCraft } from './ponte-cadcraft';

const FERRAMENTA = 'desenho-2d';

/**
 * Desenho 2D: o CADCraft completo na área de trabalho e a IA desta ferramenta no chat. Os dois trabalham
 * no mesmo arquivo, o DXF da versão guardado no Castwork: a IA desenha nele pelo MCP (no servidor) e a
 * tela recarrega o desenho; o que o joalheiro desenha à mão é salvo antes de cada mensagem à IA.
 */
@Component({
  selector: 'cw-desenho-2d',
  imports: [FerramentaIaComponent, HistoricoDesenhoComponent],
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
  protected readonly pensando = signal(false);
  /** Recarregando o desenho no CADCraft (abrir, salvar, trazer o que a IA fez). */
  protected readonly sincronizando = signal<string | null>('Abrindo o CADCraft...');
  protected readonly conversa = signal<ConversaFerramentaDto>({ mensagens: [], arquivoId: null });
  /** Versões do arquivo no painel de histórico; nulo com o painel fechado. */
  protected readonly historico = signal<readonly VersaoDoArquivoDto[] | null>(null);
  /** Fala enviada, mostrada enquanto a IA trabalha. */
  private readonly pendente = signal<MensagemFerramenta | null>(null);

  protected readonly mensagens = computed<readonly MensagemFerramenta[]>(() => {
    const projetoId = Number(this.id());
    const versaoId = Number(this.versaoId());
    const lista: MensagemFerramenta[] = this.conversa().mensagens.map((m) => ({
      id: m.ordem,
      papel: m.papel === 'Joalheiro' ? 'joalheiro' : 'assistente',
      texto: m.texto ?? '',
      imagens: m.imagens.map((chave) => this.ferramentas.caminhoDaImagem(projetoId, versaoId, chave)),
      acao: m.acao ?? undefined,
    }));
    const pendente = this.pendente();
    return pendente ? [...lista, pendente] : lista;
  });

  private readonly iframe = viewChild.required<ElementRef<HTMLIFrameElement>>('cadcraft');
  private ponte: PonteCadCraft | null = null;

  protected async iniciar(): Promise<void> {
    if (this.ponte) return;
    this.ponte = new PonteCadCraft(this.iframe().nativeElement, (arquivo) => void this.guardar(arquivo, 'Desenho salvo no Castwork.'));
    const [conversa, arquivo] = await Promise.all([
      firstValueFrom(this.ferramentas.getConversa(this.projetoId, this.numeroDaVersao, FERRAMENTA)),
      firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.numeroDaVersao, FERRAMENTA)),
    ]);
    this.conversa.set(conversa);
    await this.abrir(arquivo?.conteudo ?? null, 'Abrindo o desenho da versão...');
  }

  protected async enviar({ texto, imagens }: FalaFerramenta): Promise<void> {
    this.pensando.set(true);
    this.pendente.set({ id: -1, papel: 'joalheiro', texto, imagens: imagens.map((i) => i.url) });
    try {
      // A IA trabalha no arquivo guardado: o que foi desenhado à mão vai antes.
      if (this.ponte?.temAlteracoes()) {
        this.sincronizando.set('Guardando o seu desenho...');
        await this.guardar(await this.ponte.salvar());
      }

      this.sincronizando.set(null);
      const anterior = this.conversa().arquivoId;
      const conversa = await firstValueFrom(
        this.ferramentas.postConversa(this.projetoId, this.numeroDaVersao, FERRAMENTA, texto || null, imagens.map((i) => i.base64)),
      );
      this.pendente.set(null);
      this.conversa.set(conversa);
      if (conversa.arquivoId !== anterior) {
        await this.trazerArquivo('Trazendo o desenho da IA...');
      }
    } catch (erro) {
      // Erros da API já viram toast no interceptor; os do CADCraft (salvar o desenho) aparecem aqui.
      if (!(erro instanceof HttpErrorResponse)) {
        this.toast.erro(erro instanceof Error ? erro.message : 'Não foi possível enviar a mensagem.');
      }

      this.pendente.set(null);
      this.sincronizando.set(null);
    } finally {
      this.pensando.set(false);
    }
  }

  protected async desfazer(ordem: number): Promise<void> {
    const conversa = await firstValueFrom(this.ferramentas.postDesfazer(this.projetoId, this.numeroDaVersao, FERRAMENTA, ordem));
    this.conversa.set(conversa);
    await this.trazerArquivo('Voltando o desenho...');
  }

  protected async apagarConversa(): Promise<void> {
    await firstValueFrom(this.ferramentas.deleteConversa(this.projetoId, this.numeroDaVersao, FERRAMENTA));
    this.conversa.update((c) => ({ ...c, mensagens: [] }));
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
      this.conversa.update((c) => ({ ...c, arquivoId }));
      this.historico.set(null);
      await this.trazerArquivo('Restaurando o desenho...');
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
    this.conversa.update((c) => ({ ...c, arquivoId }));
    if (aviso) this.toast.sucesso(aviso);
  }

  private async trazerArquivo(mensagem: string): Promise<void> {
    const arquivo = await firstValueFrom(this.ferramentas.getArquivo(this.projetoId, this.numeroDaVersao, FERRAMENTA));
    await this.abrir(arquivo?.conteudo ?? null, mensagem);
  }

  /**
   * Sem arquivo na versão, abre um DXF vazio em milímetros com nome: um desenho "Drawing1" sem nome
   * faria o ⌘S abrir o Salvar como, e o Castwork não receberia o arquivo.
   */
  private async abrir(arquivo: Blob | null, mensagem: string): Promise<void> {
    this.sincronizando.set(mensagem);
    try {
      // Arquivo estático do próprio front (fora da API): fetch direto, sem os interceptors.
      const desenho = arquivo ?? (await (await fetch('/desenho-vazio.dxf')).blob());
      await this.ponte?.carregar(desenho);
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

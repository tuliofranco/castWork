import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ImagemGuardadaDto, VistaDaImagem } from '@data/assistente/dto/ferramenta.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { nomeDeArquivoDaPeca } from '@shared/arquivos/nome-de-arquivo';
import { DownloadService } from '@shared/services/download.service';
import { type Observable, firstValueFrom, map } from 'rxjs';

/** Como cada ferramenta aparece: nome e extensão do arquivo baixado. */
const FERRAMENTAS: Record<string, { readonly nome: string; readonly extensao: string }> = {
  'desenho-2d': { nome: 'Desenho 2D', extensao: 'dxf' },
  'modelo-3d': { nome: 'Modelo 3D', extensao: 'json' },
};

/** O nome de cada vista nos botões. */
const NOMES_DAS_VISTAS: Record<VistaDaImagem, string> = { iso: 'Iso', frente: 'Frente', topo: 'Topo', lado: 'Lado', '2d': '2D' };

const ZOOM_MINIMO = 0.25;
const ZOOM_MAXIMO = 16;

/**
 * O desenho de uma peça **só para ver**, em fundo preto, sem abrir o CADCraft ou o SolidCraft, e deixa baixar o arquivo.
 * Mostra as imagens guardadas em alta qualidade (geradas depois de salvar): no 3D, uma por vista (Iso, Frente, Topo, Lado);
 * no 2D, o SVG. Sem imagem guardada (ainda gerando, ou peça publicada antes), usa a prévia sob demanda. Zoom pela roda do
 * mouse (no ponto do cursor), arrastar move, duplo clique ou "Ajustar" volta ao todo. Duas origens:
 * - peça da biblioteca (rota `pecas/:id/desenhos/:ferramenta`);
 * - peça do projeto (rota `projetos/:id/pecas/:pecaId/desenhos/:ferramenta`), aberta pela composição da versão: ali `id`
 *   é o projeto e `?versao=` faz o voltar levar de novo à versão. Mostra o **arquivo final** da peça (sem final, a peça
 *   não tem este desenho). Editar continua na página da peça.
 * O título é "Elo oval · Modelo 3D", e os arquivos baixados levam o nome da peça ("Elo oval.dxf", "Elo oval - Iso.png").
 */
@Component({
  selector: 'cw-desenho-da-peca',
  imports: [RouterLink],
  template: `
    <header class="topo">
      <a class="btn btn-sutil" [routerLink]="voltar()"><i class="bi bi-chevron-left" aria-hidden="true"></i> {{ versao() ? 'Versão' : 'Peça' }}</a>
      <h1>{{ nome() }}</h1>
      @if (vistas().length > 1) {
        <div class="vistas" role="group" aria-label="Vista">
          @for (v of vistas(); track v) {
            <button type="button" class="btn btn-sutil" [class.ativa]="v === vista()" [attr.aria-pressed]="v === vista()" (click)="mostrar(v)">
              {{ nomeDaVista(v) }}
            </button>
          }
        </div>
      }
      <span class="espaco"></span>
      @if (imagem()) {
        <div class="zoom" role="group" aria-label="Zoom">
          <button type="button" class="btn btn-sutil" aria-label="Diminuir o zoom" (click)="aproximar(1 / 1.25)"><i class="bi bi-dash-lg" aria-hidden="true"></i></button>
          <button type="button" class="btn btn-sutil escala" title="Ajustar à tela" (click)="ajustar()">{{ porcentagem() }}%</button>
          <button type="button" class="btn btn-sutil" aria-label="Aumentar o zoom" (click)="aproximar(1.25)"><i class="bi bi-plus-lg" aria-hidden="true"></i></button>
        </div>
      }
      @if (vista(); as v) {
        <button type="button" class="btn btn-sutil" [disabled]="!imagem()" (click)="baixarImagem(v)">
          <i class="bi bi-image" aria-hidden="true"></i> Baixar imagem
        </button>
      }
      <button type="button" class="btn btn-secundario" [disabled]="!temArquivo()" (click)="baixar()">
        <i class="bi bi-download" aria-hidden="true"></i> Baixar
      </button>
    </header>
    <div
      class="prancheta"
      [class.arrastando]="arrastando()"
      (wheel)="rolar($event)"
      (pointerdown)="comecarArrasto($event)"
      (pointermove)="arrastar($event)"
      (pointerup)="terminarArrasto($event)"
      (pointercancel)="terminarArrasto($event)"
      (dblclick)="ajustar()"
    >
      @if (imagem(); as url) {
        <img
          [src]="url"
          [alt]="nome()"
          draggable="false"
          [class.cad]="ferramenta() === 'desenho-2d'"
          [style.transform]="transformacao()"
        />
      } @else {
        <p class="estado" role="status">{{ estado() }}</p>
      }
    </div>
  `,
  styles: `
    :host { display: flex; flex-direction: column; height: 100dvh; background: #000; color: #e6e6e6; }
    .topo { display: flex; align-items: center; gap: 0.75rem; padding: 0.5rem 1rem; border-bottom: 1px solid #1f1f1f; flex-wrap: wrap; }
    h1 { margin: 0; font-size: 1.125rem; }
    .espaco { flex: 1; }
    .vistas, .zoom { display: flex; gap: 0.25rem; }
    .vistas .ativa { background: #2a2a2a; color: #fff; }
    .escala { min-width: 4.5rem; font-variant-numeric: tabular-nums; }
    .prancheta { flex: 1; display: flex; align-items: center; justify-content: center; min-height: 0; padding: 1.5rem; overflow: hidden; cursor: grab; touch-action: none; }
    .prancheta.arrastando { cursor: grabbing; }
    img { max-width: 100%; max-height: 100%; object-fit: contain; transform-origin: center center; user-select: none; }
    img.cad { width: 100%; height: 100%; }
    .estado { margin: 0; color: #a0a0a0; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DesenhoDaPecaComponent {
  private readonly pecas = inject(PecasService);
  private readonly ferramentas = inject(FerramentasService);
  private readonly download = inject(DownloadService);
  private readonly projetos = inject(ProjetosService);

  /** `:id` e `:ferramenta` (`desenho-2d` ou `modelo-3d`) da rota (withComponentInputBinding). */
  readonly id = input.required<string>();
  readonly ferramenta = input.required<string>();
  /** Só na rota da peça do projeto: com ele, `id` é o projeto. */
  readonly pecaId = input<string>();
  /** `?versao=` quando aberto pela composição da versão: voltar leva de novo à versão, não à peça. */
  readonly versao = input<string>();

  /** O nome da peça (carregado depois de abrir); nulo enquanto não chega. */
  protected readonly nomeDaPeca = signal<string | null>(null);
  protected readonly nome = computed(() => {
    const ferramenta = FERRAMENTAS[this.ferramenta()]?.nome ?? 'Desenho';
    const peca = this.nomeDaPeca();
    return peca ? `${peca} · ${ferramenta}` : ferramenta;
  });
  protected readonly voltar = computed(() => {
    const pecaId = this.pecaId();
    const versao = this.versao();
    if (pecaId && versao) return ['/projetos', this.id(), 'versoes', versao];
    return pecaId ? ['/projetos', this.id(), 'pecas', pecaId] : ['/pecas', this.id()];
  });
  protected readonly estado = signal('Abrindo o desenho...');
  protected readonly imagem = signal<string | null>(null);
  protected readonly temArquivo = signal(false);
  /** As vistas guardadas, na ordem do servidor; vazia quando a tela usa a prévia sob demanda. */
  protected readonly vistas = signal<readonly VistaDaImagem[]>([]);
  protected readonly vista = signal<VistaDaImagem | null>(null);

  protected readonly escala = signal(1);
  protected readonly deslocamento = signal({ x: 0, y: 0 });
  protected readonly arrastando = signal(false);
  protected readonly porcentagem = computed(() => Math.round(this.escala() * 100));
  protected readonly transformacao = computed(() => {
    const { x, y } = this.deslocamento();
    return `translate(${x}px, ${y}px) scale(${this.escala()})`;
  });

  /** Cada vista é baixada uma vez: trocar de vista volta à imagem já carregada. */
  private readonly carregadas = new Map<VistaDaImagem, string>();
  /** A imagem guardada como veio do servidor (o SVG sem a troca para o fundo escuro), para baixar. */
  private readonly originais = new Map<VistaDaImagem, Blob>();
  private arrasto: { readonly x: number; readonly y: number; readonly origem: { x: number; y: number } } | null = null;

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      for (const url of this.carregadas.values()) URL.revokeObjectURL(url);
      const url = this.imagem();
      if (url && ![...this.carregadas.values()].includes(url)) URL.revokeObjectURL(url);
    });
    queueMicrotask(() => {
      void this.abrir();
      void this.carregarNome();
    });
  }

  protected nomeDaVista(vista: VistaDaImagem): string {
    return NOMES_DAS_VISTAS[vista] ?? vista;
  }

  protected async mostrar(vista: VistaDaImagem): Promise<void> {
    this.vista.set(vista);
    this.ajustar();
    const guardada = this.carregadas.get(vista);
    if (guardada) {
      this.imagem.set(guardada);
      return;
    }
    try {
      const blob = await firstValueFrom(this.imagemGuardada(vista));
      if (!blob) return;
      const url = URL.createObjectURL(await this.paraFundoEscuro(blob));
      this.originais.set(vista, blob);
      this.carregadas.set(vista, url);
      if (this.vista() === vista) this.imagem.set(url);
    } catch {
      this.estado.set('Não foi possível abrir esta vista.');
    }
  }

  protected ajustar(): void {
    this.escala.set(1);
    this.deslocamento.set({ x: 0, y: 0 });
  }

  /** Zoom pelos botões, a partir do centro. */
  protected aproximar(fator: number): void {
    this.zoomNoPonto(fator, 0, 0);
  }

  /** Zoom pela roda, mantendo parado o ponto sob o cursor. */
  protected rolar(evento: WheelEvent): void {
    if (!this.imagem()) return;
    evento.preventDefault();
    const area = (evento.currentTarget as HTMLElement).getBoundingClientRect();
    const fator = Math.exp(-evento.deltaY * 0.0015);
    this.zoomNoPonto(fator, evento.clientX - (area.left + area.width / 2), evento.clientY - (area.top + area.height / 2));
  }

  protected comecarArrasto(evento: PointerEvent): void {
    if (!this.imagem() || evento.button !== 0) return;
    this.arrasto = { x: evento.clientX, y: evento.clientY, origem: this.deslocamento() };
    this.arrastando.set(true);
    (evento.currentTarget as HTMLElement).setPointerCapture?.(evento.pointerId);
  }

  protected arrastar(evento: PointerEvent): void {
    if (!this.arrasto) return;
    const { x, y, origem } = this.arrasto;
    this.deslocamento.set({ x: origem.x + evento.clientX - x, y: origem.y + evento.clientY - y });
  }

  protected terminarArrasto(evento: PointerEvent): void {
    if (!this.arrasto) return;
    this.arrasto = null;
    this.arrastando.set(false);
    (evento.currentTarget as HTMLElement).releasePointerCapture?.(evento.pointerId);
  }

  /** O arquivo com o nome da peça ("Elo oval.dxf"). */
  protected async baixar(): Promise<void> {
    const arquivo = await firstValueFrom(this.arquivo());
    const extensao = FERRAMENTAS[this.ferramenta()]?.extensao ?? 'bin';
    if (arquivo) this.download.salvar(arquivo, nomeDeArquivoDaPeca(this.nomeDaPeca(), extensao));
  }

  /** A imagem da vista à mostra, como foi guardada, com o nome da peça e da vista ("Elo oval - Iso.png"). */
  protected baixarImagem(vista: VistaDaImagem): void {
    const imagem = this.originais.get(vista);
    if (imagem) this.download.salvar(imagem, nomeDeArquivoDaPeca(this.nomeDaPeca(), imagem.type.includes('svg') ? 'svg' : 'png', this.nomeDaVista(vista)));
  }

  /** `(px, py)`: o ponto em relação ao centro da prancheta, que fica parado na tela. */
  private zoomNoPonto(fator: number, px: number, py: number): void {
    const atual = this.escala();
    const nova = Math.min(ZOOM_MAXIMO, Math.max(ZOOM_MINIMO, atual * fator));
    const real = nova / atual;
    const { x, y } = this.deslocamento();
    this.escala.set(nova);
    this.deslocamento.set({ x: px - (px - x) * real, y: py - (py - y) * real });
  }

  /** O nome da peça para o título e os arquivos baixados; sem ele, o título fica só com a ferramenta. */
  private async carregarNome(): Promise<void> {
    const pecaId = this.pecaId();
    try {
      const nome = pecaId
        ? (await firstValueFrom(this.projetos.getPecasDoProjeto(Number(this.id()), false)))?.find((p) => p.id === Number(pecaId))?.nome
        : (await firstValueFrom(this.pecas.getPeca(Number(this.id()), { comHistorico: false, comValor: false })))?.nome;
      this.nomeDaPeca.set(nome ?? null);
    } catch {
      this.nomeDaPeca.set(null);
    }
  }

  /** Na peça do projeto, o arquivo final (o que vale para a peça), não o mais recente. */
  private arquivo() {
    const pecaId = this.pecaId();
    return pecaId
      ? this.ferramentas.getArquivo(Number(this.id()), { pecaId: Number(pecaId) }, this.ferramenta(), true).pipe(map((a) => a?.conteudo ?? null))
      : this.pecas.getDesenho(Number(this.id()), this.ferramenta());
  }

  private imagensGuardadas(): Observable<readonly ImagemGuardadaDto[]> {
    const pecaId = this.pecaId();
    return pecaId
      ? this.ferramentas.getImagensDoArquivo(Number(this.id()), { pecaId: Number(pecaId) }, this.ferramenta(), true).pipe(map((r) => r.imagens))
      : this.pecas.getImagensDoDesenho(Number(this.id()), this.ferramenta());
  }

  private imagemGuardada(vista: VistaDaImagem): Observable<Blob | null> {
    const pecaId = this.pecaId();
    return pecaId
      ? this.ferramentas.getImagemDoArquivo(Number(this.id()), { pecaId: Number(pecaId) }, this.ferramenta(), vista, undefined, true)
      : this.pecas.getImagemDoDesenho(Number(this.id()), this.ferramenta(), vista);
  }

  private async abrir(): Promise<void> {
    try {
      const guardadas = await firstValueFrom(this.imagensGuardadas()).catch(() => [] as readonly ImagemGuardadaDto[]);
      const vistas = [...new Set(guardadas.map((i) => i.vista))];
      if (vistas.length > 0) {
        this.vistas.set(vistas);
        this.temArquivo.set(true);
        await this.mostrar(vistas[0]);
        if (this.imagem()) return;
        this.vistas.set([]);
      }
      await this.abrirPrevia();
    } catch {
      this.estado.set('Não foi possível abrir o desenho.');
    }
  }

  /** Sem imagem guardada: a prévia gerada na hora pelo programa da ferramenta. */
  private async abrirPrevia(): Promise<void> {
    const pecaId = this.pecaId();
    const previa = await firstValueFrom(
      pecaId
        ? this.ferramentas.getPreviaAtual(Number(this.id()), { pecaId: Number(pecaId) }, this.ferramenta(), true)
        : this.pecas.getPreviaDoDesenho(Number(this.id()), this.ferramenta()),
    );
    if (!previa) {
      const arquivo = await firstValueFrom(this.arquivo());
      this.temArquivo.set(!!arquivo);
      this.estado.set(arquivo ? 'A prévia deste desenho não está disponível. Baixe o arquivo para ver.' : 'Esta peça não tem este desenho.');
      return;
    }
    this.temArquivo.set(true);
    this.imagem.set(URL.createObjectURL(await this.paraFundoEscuro(previa)));
  }

  /** O SVG do CADCraft vem em fundo branco; o PNG do 3D já serve como está. */
  private async paraFundoEscuro(imagem: Blob): Promise<Blob> {
    return imagem.type.includes('svg') ? new Blob([emFundoEscuro(await imagem.text())], { type: 'image/svg+xml' }) : imagem;
  }
}

/**
 * O SVG do CADCraft vem em fundo branco (o `render` e o `save` não têm opção de fundo), mas as cores de CAD (amarelo,
 * verde, ciano) são pensadas para fundo escuro: troca só o branco pelo preto e o preto (linhas "por camada") pelo branco,
 * e as cotas ficam nas cores originais.
 */
export function emFundoEscuro(svg: string): string {
  const branco = /#(?:fff|ffffff)\b|\bwhite\b|rgb\(\s*255\s*,\s*255\s*,\s*255\s*\)/gi;
  const preto = /#(?:000|000000)\b|\bblack\b|rgb\(\s*0\s*,\s*0\s*,\s*0\s*\)/gi;
  return svg.replace(branco, '__FUNDO__').replace(preto, '#ffffff').replace(/__FUNDO__/g, '#000000');
}

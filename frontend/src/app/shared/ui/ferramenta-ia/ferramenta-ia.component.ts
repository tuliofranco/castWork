import { ChangeDetectionStrategy, Component, ElementRef, afterRenderEffect, effect, input, output, signal, untracked, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AnexosImagemComponent } from '../anexos-imagem/anexos-imagem.component';
import { ImagemProtegidaComponent } from '../imagem-protegida.component';
import { type AndamentoDoTurno, AndamentoIaComponent } from './andamento-ia.component';
import { ESCOLHA_PADRAO, ESFORCOS, escolhaPadrao, type EscolhaDaIa, EscolhaIaComponent, type EsforcoIa, MODELOS, type ModeloIa } from './escolha-ia.component';
import { type ArquivoDoProjeto, ArquivosDoProjetoComponent, chaveDoArquivo } from './arquivos-do-projeto.component';
import type { ImagemAnexada } from '../anexos-imagem/preparar-imagem';

/** Uma fala do chat. `acao` é o que a IA executou pelo MCP, mostrado como cartão com desfazer. */
export interface MensagemFerramenta {
  readonly id: number;
  readonly papel: 'joalheiro' | 'assistente';
  readonly texto: string;
  /** Imagens: caminho da API ou URL local (`data:`, recém-anexada). */
  readonly imagens?: readonly string[];
  readonly acao?: { readonly descricao: string; readonly desfeita: boolean };
  /** Resposta final da IA: resumo do pensamento, passos e tokens do turno ("Como a IA trabalhou"). */
  readonly pensamento?: string | null;
  readonly passos?: readonly string[] | null;
  readonly tokens?: number | null;
  /** Nomes dos arquivos do projeto anexados à fala. */
  readonly anexos?: readonly string[];
}

/** Um chat da conversa, na lista de conversas anteriores. */
export interface ChatDaConversa {
  readonly numero: number;
  readonly ultimaMensagemEm: string;
  readonly titulo: string | null;
  readonly mensagens: number;
  readonly atual: boolean;
}

/** O que o joalheiro manda ao assistente. */
export interface FalaFerramenta {
  readonly texto: string;
  readonly imagens: readonly ImagemAnexada[];
  /** Arquivos do projeto anexados ("Do projeto"). */
  readonly arquivos: readonly ArquivoDoProjeto[];
  /** Escolha do joalheiro no chat; a abertura vai sem (o padrão do servidor). */
  readonly modelo?: ModeloIa;
  readonly esforco?: EsforcoIa;
}


/** Até quantos arquivos do projeto por mensagem (o mesmo limite da API). */
const ARQUIVOS_POR_MENSAGEM = 3;

const LARGURA_PADRAO = 416;
const LARGURA_MINIMA = 288;
const CHAVE_LARGURA = 'castwork.ferramenta-ia.largura-chat';
const CHAVE_ESCOLHA = 'castwork.ferramenta-ia.modelo.';

/**
 * Tela padrão das ferramentas com IA: barra fina no topo (voltar, nome, ações e ⋯ com o histórico),
 * área de trabalho à esquerda com as propriedades embaixo, e o assistente à direita.
 * Cada ferramenta só preenche a área (`[area]`), as propriedades (`[propriedades]`) e as ações (`[acoes]`).
 * A IA aplica direto; cada ação vira um cartão no chat com Desfazer.
 */
@Component({
  selector: 'cw-ferramenta-ia',
  imports: [FormsModule, RouterLink, AnexosImagemComponent, ArquivosDoProjetoComponent, ImagemProtegidaComponent, AndamentoIaComponent, EscolhaIaComponent],
  template: `
    <header class="barra">
      <a class="voltar" [routerLink]="voltarPara()" [attr.aria-label]="'Voltar para ' + voltarRotulo()">
        <i class="bi bi-chevron-left" aria-hidden="true"></i><span class="rotulo-voltar">{{ voltarRotulo() }}</span>
      </a>
      <h1>{{ titulo() }}</h1>
      <div class="acoes">
        <ng-content select="[acoes]" />
        @if (minimizado()) {
          <button type="button" class="btn btn-secundario" (click)="minimizado.set(false)"><i class="bi bi-stars" aria-hidden="true"></i> Assistente</button>
        }
        <div class="menu">
          <button type="button" class="btn btn-sutil btn-icone" aria-label="Mais opções" [attr.aria-expanded]="menuAberto()" (click)="menuAberto.set(!menuAberto())">
            <i class="bi bi-three-dots" aria-hidden="true"></i>
          </button>
          @if (menuAberto()) {
            <div class="menu-painel" role="menu">
              @if (!chatUnico()) {
                <button type="button" role="menuitem" [disabled]="pensando() || mensagens().length === 0" (click)="pedirNovoChat()"><i class="bi bi-plus-lg" aria-hidden="true"></i> Novo chat</button>
                <button type="button" role="menuitem" (click)="pedirChats()"><i class="bi bi-clock-history" aria-hidden="true"></i> Conversas anteriores</button>
              }
              <button type="button" role="menuitem" class="perigo" (click)="pedirApagar()"><i class="bi bi-trash3" aria-hidden="true"></i> Apagar este chat</button>
              <ng-content select="[menu]" />
            </div>
          }
        </div>
      </div>
    </header>

    <div class="corpo" [class.chat-minimizado]="minimizado()" [class.chat-maximizado]="maximizado()" [style.--largura-chat.px]="largura()">
      <section class="trabalho" aria-label="Área de trabalho">
        <div class="area"><ng-content select="[area]" /></div>
        <div class="propriedades"><ng-content select="[propriedades]" /></div>
      </section>

      <aside class="assistente" [class.aberto]="chatAberto()" aria-labelledby="titulo-assistente">
        @if (!maximizado()) {
          <div
            class="divisor"
            role="separator"
            aria-orientation="vertical"
            aria-label="Largura do assistente"
            tabindex="0"
            [attr.aria-valuenow]="largura()"
            (pointerdown)="comecarArrastar($event)"
            (keydown)="teclaNoDivisor($event)"
          ></div>
        }
        <div class="topo-assistente">
          <button type="button" class="alca" [attr.aria-expanded]="chatAberto()" (click)="chatAberto.set(!chatAberto())">
            <h2 id="titulo-assistente"><i class="bi bi-stars" aria-hidden="true"></i> Assistente</h2>
            <i class="bi seta" [class]="chatAberto() ? 'bi-chevron-down' : 'bi-chevron-up'" aria-hidden="true"></i>
          </button>
          @if (!chatUnico()) {
            <button type="button" class="btn btn-sutil btn-icone" aria-label="Novo chat" title="Novo chat" [disabled]="pensando() || mensagens().length === 0" (click)="pedirNovoChat()">
              <i class="bi bi-plus-lg" aria-hidden="true"></i>
            </button>
            <button type="button" class="btn btn-sutil btn-icone" aria-label="Conversas anteriores" title="Conversas anteriores" (click)="pedirChats()">
              <i class="bi bi-clock-history" aria-hidden="true"></i>
            </button>
          }
          <button type="button" class="btn btn-sutil btn-icone" aria-label="Apagar este chat" title="Apagar este chat" [disabled]="pensando() || mensagens().length === 0" (click)="pedirApagar()">
            <i class="bi bi-trash3" aria-hidden="true"></i>
          </button>
          <button
            type="button"
            class="btn btn-sutil btn-icone maximizar"
            [attr.aria-label]="maximizado() ? 'Restaurar tamanho do assistente' : 'Maximizar assistente'"
            [title]="maximizado() ? 'Restaurar' : 'Maximizar'"
            [attr.aria-pressed]="maximizado()"
            (click)="maximizado.set(!maximizado())"
          >
            <i class="bi" [class]="maximizado() ? 'bi-fullscreen-exit' : 'bi-arrows-fullscreen'" aria-hidden="true"></i>
          </button>
          <button type="button" class="btn btn-sutil btn-icone minimizar" aria-label="Minimizar assistente" title="Minimizar" (click)="minimizar()">
            <i class="bi bi-dash-lg" aria-hidden="true"></i>
          </button>
        </div>
        @if (confirmandoApagar()) {
          <div class="confirmar" role="alertdialog" aria-labelledby="texto-apagar">
            <span id="texto-apagar">Apagar este chat? As conversas anteriores e o desenho não mudam.</span>
            <button type="button" class="btn btn-perigo-sutil" (click)="apagar()">Apagar</button>
            <button type="button" class="btn btn-sutil" (click)="confirmandoApagar.set(false)">Cancelar</button>
          </div>
        }
        <ol class="conversa" aria-live="polite">
          @if (mensagens().length === 0 && boasVindas()) {
            <li class="fala fala-assistente"><p>{{ boasVindas() }}</p></li>
          }
          @for (m of mensagens(); track m.id) {
            <li class="fala" [class]="'fala-' + m.papel">
              @if (m.imagens?.length) {
                <div class="imagens">
                  @for (src of m.imagens; track src) {
                    <cw-imagem-protegida [src]="src" [rotulo]="m.papel === 'joalheiro' ? 'Imagem enviada' : 'O desenho como a IA viu'" />
                  }
                </div>
              }
              @if (m.anexos?.length) {
                <ul class="arquivos-anexados">
                  @for (nome of m.anexos; track $index) {
                    <li><i class="bi bi-paperclip" aria-hidden="true"></i> {{ nome }}</li>
                  }
                </ul>
              }
              @if (m.texto) {
                <p>{{ m.texto }}</p>
              }
              @if (m.passos?.length || m.pensamento) {
                <details class="como-trabalhou">
                  <summary>Como a IA trabalhou@if (m.tokens) {<span class="tokens"> · {{ tokensLegiveis(m.tokens) }}</span>}</summary>
                  @if (m.passos?.length) {
                    <ol>
                      @for (passo of m.passos; track $index) {
                        <li>{{ passo }}</li>
                      }
                    </ol>
                  }
                  @if (m.pensamento) {
                    <p class="pensamento">{{ m.pensamento }}</p>
                  }
                </details>
              }
              @if (m.acao; as acao) {
                <div class="cartao-acao" [class.desfeita]="acao.desfeita">
                  <i class="bi" [class]="acao.desfeita ? 'bi-arrow-counterclockwise' : 'bi-check2-circle'" aria-hidden="true"></i>
                  <span>{{ acao.desfeita ? 'Desfeito: ' : 'Aplicou: ' }}{{ acao.descricao }}</span>
                  @if (!acao.desfeita) {
                    <button type="button" class="btn btn-sutil" (click)="desfazer.emit(m.id)">Desfazer</button>
                  }
                </div>
              }
            </li>
          }
          @if (pensando()) {
            <li class="fala fala-assistente pensando">
              @if (andamento(); as a) {
                <cw-andamento-ia [andamento]="a" />
              } @else {
                <p><span class="spinner-grow spinner-grow-sm" aria-hidden="true"></span> Trabalhando no desenho...</p>
              }
            </li>
          }
          <li #fim aria-hidden="true"></li>
        </ol>
        <form class="entrada" (ngSubmit)="enviarFala()" (dragover)="$event.preventDefault()" (drop)="soltar($event, anexador)">
          <cw-anexos-imagem #anexador class="anexos" [(imagens)]="anexos" />
          @if (arquivos().length > 0) {
            <ul class="anexos arquivos-anexados">
              @for (a of arquivos(); track chaveDoArquivo(a)) {
                <li>
                  <i class="bi bi-paperclip" aria-hidden="true"></i> {{ a.nome }}
                  <button type="button" class="remover" [attr.aria-label]="'Remover ' + a.nome" (click)="removerArquivo(a)"><i class="bi bi-x" aria-hidden="true"></i></button>
                </li>
              }
            </ul>
          }
          <div class="anexar">
            <button
              type="button"
              class="btn btn-sutil btn-icone"
              aria-label="Anexar"
              title="Anexar"
              aria-haspopup="menu"
              [attr.aria-expanded]="menuAnexoAberto()"
              [disabled]="pensando()"
              (click)="anexar(anexador)"
            >
              <i class="bi bi-paperclip" aria-hidden="true"></i>
            </button>
            @if (menuAnexoAberto()) {
              <div class="menu-painel" role="menu">
                <button type="button" role="menuitem" (click)="menuAnexoAberto.set(false); anexador.escolher()">
                  <i class="bi bi-laptop" aria-hidden="true"></i> Do computador
                </button>
                <button type="button" role="menuitem" (click)="abrirArquivosDoProjeto()">
                  <i class="bi bi-folder2-open" aria-hidden="true"></i> Do projeto
                </button>
              </div>
            }
          </div>
          <label class="visually-hidden" for="fala-ferramenta">Mensagem para o assistente</label>
          <textarea
            id="fala-ferramenta"
            name="fala"
            class="form-control"
            rows="1"
            placeholder="Peça uma mudança no desenho..."
            [(ngModel)]="fala"
            (keydown)="tecla($event)"
            (paste)="anexador.colar($event)"
          ></textarea>
          <button type="submit" class="btn btn-primary btn-icone" aria-label="Enviar" [disabled]="(!fala.trim() && anexos().length === 0 && arquivos().length === 0) || pensando()">
            <i class="bi bi-arrow-up" aria-hidden="true"></i>
          </button>
          <cw-escolha-ia [desabilitado]="pensando()" [escolha]="escolha()" (escolhaChange)="escolher($event)" />
        </form>
      </aside>
    </div>

    <cw-arquivos-do-projeto
      [arquivos]="arquivosDisponiveis()"
      [anexados]="arquivos()"
      [maximo]="arquivosPorMensagem"
      (escolher)="adicionarArquivos($event)"
      (fechado)="arquivosDisponiveis.set(null)"
    />

    <dialog #historico class="historico" aria-labelledby="titulo-historico" (close)="fecharChats.emit()">
      <header>
        <h2 id="titulo-historico">Conversas anteriores</h2>
        <button type="button" class="btn btn-sutil btn-icone" aria-label="Fechar" (click)="historico.close()"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
      </header>
      @if (chats(); as lista) {
        <ul class="chats">
          @for (c of lista; track c.numero) {
            <li>
              <button type="button" [disabled]="pensando() && !c.atual" (click)="abrirChat.emit(c.numero)">
                <span class="titulo">{{ c.titulo ?? 'Conversa só com imagens' }}</span>
                <small>{{ quando(c.ultimaMensagemEm) }} · {{ c.mensagens }} {{ c.mensagens === 1 ? 'mensagem' : 'mensagens' }}@if (c.atual) { · <strong>em andamento</strong>}</small>
              </button>
            </li>
          } @empty {
            <li class="vazio">Nenhuma conversa ainda.</li>
          }
        </ul>
      }
    </dialog>
  `,
  styleUrl: './ferramenta-ia.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FerramentaIaComponent {
  readonly titulo = input.required<string>();
  readonly voltarPara = input.required<string>();
  readonly voltarRotulo = input('Voltar');
  readonly mensagens = input.required<readonly MensagemFerramenta[]>();
  readonly pensando = input(false);
  /** O turno ao vivo (pensamento, passos e texto); nulo mostra só "Trabalhando...". */
  readonly andamento = input<AndamentoDoTurno | null>(null);

  readonly enviar = output<FalaFerramenta>();
  readonly desfazer = output<number>();
  readonly apagarConversa = output();
  readonly novoChat = output();
  /** Pede a lista de chats; o modal abre quando `chats` chega. */
  readonly verChats = output();
  /** Abre o chat na conversa com a IA (ele vira o em andamento). */
  readonly abrirChat = output<number>();
  readonly fecharChats = output();

  /** Lista de conversas anteriores; com valor, o modal fica aberto. */
  readonly chats = input<readonly ChatDaConversa[] | null>(null);
  /** Lista os arquivos do projeto para anexar ("Do projeto"); sem ela, anexar só abre o seletor de imagens. */
  readonly listarArquivosDoProjeto = input<(() => Promise<readonly ArquivoDoProjeto[]>) | null>(null);
  /** Mensagem fixa do assistente enquanto o chat está vazio (não vai para a IA nem para o banco). */
  readonly boasVindas = input<string | null>(null);
  /** Código da ferramenta: o modelo e o esforço escolhidos ficam lembrados por ferramenta neste navegador. */
  readonly ferramenta = input('');
  /** Uma conversa só (na peça do projeto): sem "Novo chat" nem "Conversas anteriores". */
  readonly chatUnico = input(false);

  protected readonly menuAberto = signal(false);
  /** No tablet em pé o assistente vira gaveta; deitado ou no computador fica sempre aberto. */
  protected readonly chatAberto = signal(false);
  /** Minimizado, o assistente sai da tela e a área de trabalho ocupa tudo. */
  protected readonly minimizado = signal(false);
  protected readonly confirmandoApagar = signal(false);
  /** Maximizado, o assistente ocupa a tela toda (a área de trabalho some até restaurar). */
  protected readonly maximizado = signal(false);
  /** Largura do assistente em px, arrastando a borda esquerda; lembrada neste navegador. */
  protected readonly largura = signal(lerLarguraSalva());
  protected readonly anexos = signal<readonly ImagemAnexada[]>([]);
  protected readonly arquivos = signal<readonly ArquivoDoProjeto[]>([]);
  /** Lista do "Do projeto"; com valor, o modal fica aberto. */
  protected readonly arquivosDisponiveis = signal<readonly ArquivoDoProjeto[] | null>(null);
  protected readonly menuAnexoAberto = signal(false);
  protected readonly arquivosPorMensagem = ARQUIVOS_POR_MENSAGEM;
  protected readonly escolha = signal<EscolhaDaIa>(ESCOLHA_PADRAO);
  protected readonly chaveDoArquivo = chaveDoArquivo;
  protected fala = '';

  private readonly historico = viewChild.required<ElementRef<HTMLDialogElement>>('historico');
  private readonly fim = viewChild.required<ElementRef<HTMLElement>>('fim');

  constructor() {
    // Cada ferramenta lembra o próprio modelo e esforço.
    effect(() => {
      const ferramenta = this.ferramenta();
      untracked(() => this.escolha.set(lerEscolhaSalva(ferramenta)));
    });

    // O modal das conversas abre quando a lista chega e fecha quando ela some.
    effect(() => {
      const dialogo = this.historico().nativeElement;
      if (this.chats() && !dialogo.open) dialogo.showModal();
      else if (!this.chats() && dialogo.open) dialogo.close();
    });

    afterRenderEffect(() => {
      this.mensagens();
      this.andamento();
      this.pensando();
      this.fim().nativeElement.scrollIntoView({ block: 'end' });
    });
  }

  protected pedirChats(): void {
    this.menuAberto.set(false);
    this.verChats.emit();
  }

  protected pedirNovoChat(): void {
    this.menuAberto.set(false);
    this.novoChat.emit();
  }

  /** "06/11 15:40". */
  protected quando(data: string): string {
    return new Date(data).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
  }

  /** 18234 → "18,2 mil tokens". */
  protected tokensLegiveis(tokens: number): string {
    return tokens >= 1000 ? `${(tokens / 1000).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} mil tokens` : `${tokens} tokens`;
  }

  protected minimizar(): void {
    this.maximizado.set(false);
    this.minimizado.set(true);
  }

  protected comecarArrastar(evento: PointerEvent): void {
    evento.preventDefault();
    const divisor = evento.target as HTMLElement;
    divisor.setPointerCapture(evento.pointerId);
    const inicioX = evento.clientX;
    const inicioLargura = this.largura();
    // O iframe do CADCraft engole o ponteiro: enquanto arrasta, ele fica sem eventos.
    document.body.classList.add('cw-arrastando');
    const mover = (e: PointerEvent) => this.definirLargura(inicioLargura + (inicioX - e.clientX));
    const soltar = () => {
      divisor.removeEventListener('pointermove', mover);
      document.body.classList.remove('cw-arrastando');
      salvarLargura(this.largura());
    };
    divisor.addEventListener('pointermove', mover);
    divisor.addEventListener('pointerup', soltar, { once: true });
    divisor.addEventListener('pointercancel', soltar, { once: true });
  }

  protected teclaNoDivisor(evento: KeyboardEvent): void {
    const passo = evento.shiftKey ? 64 : 16;
    if (evento.key === 'ArrowLeft') this.definirLargura(this.largura() + passo);
    else if (evento.key === 'ArrowRight') this.definirLargura(this.largura() - passo);
    else return;
    evento.preventDefault();
    salvarLargura(this.largura());
  }

  protected soltar(evento: DragEvent, anexador: AnexosImagemComponent): void {
    evento.preventDefault();
    void anexador.adicionar(evento.dataTransfer?.files ?? []);
  }

  protected pedirApagar(): void {
    this.menuAberto.set(false);
    this.minimizado.set(false);
    this.chatAberto.set(true);
    this.confirmandoApagar.set(true);
  }

  protected apagar(): void {
    this.confirmandoApagar.set(false);
    this.apagarConversa.emit();
  }

  protected enviarFala(): void {
    const texto = this.fala.trim();
    if ((!texto && this.anexos().length === 0 && this.arquivos().length === 0) || this.pensando()) return;
    this.enviar.emit({ texto, imagens: this.anexos(), arquivos: this.arquivos(), ...this.escolha() });
    this.fala = '';
    this.anexos.set([]);
    this.arquivos.set([]);
  }

  /** Com "Do projeto" disponível, o clipe abre o menu; sem, vai direto ao seletor de imagens. */
  protected anexar(anexador: AnexosImagemComponent): void {
    if (this.listarArquivosDoProjeto()) this.menuAnexoAberto.update((aberto) => !aberto);
    else anexador.escolher();
  }

  protected async abrirArquivosDoProjeto(): Promise<void> {
    this.menuAnexoAberto.set(false);
    const listar = this.listarArquivosDoProjeto();
    if (!listar) return;
    const anexados = new Set(this.arquivos().map(chaveDoArquivo));
    this.arquivosDisponiveis.set((await listar()).filter((a) => !anexados.has(chaveDoArquivo(a))));
  }

  protected adicionarArquivos(escolhidos: readonly ArquivoDoProjeto[]): void {
    this.arquivos.update((lista) => [...lista, ...escolhidos].slice(0, ARQUIVOS_POR_MENSAGEM));
  }

  protected removerArquivo(arquivo: ArquivoDoProjeto): void {
    this.arquivos.update((lista) => lista.filter((a) => chaveDoArquivo(a) !== chaveDoArquivo(arquivo)));
  }

  /** Entre o mínimo útil para ler o chat e 70% da tela, para a área de trabalho não sumir. */
  private definirLargura(px: number): void {
    this.largura.set(Math.round(Math.min(Math.max(px, LARGURA_MINIMA), window.innerWidth * 0.7)));
  }

  protected escolher(escolha: EscolhaDaIa): void {
    this.escolha.set(escolha);
    salvarEscolha(this.ferramenta(), this.escolha());
  }

  protected tecla(evento: KeyboardEvent): void {
    if (evento.key === 'Enter' && !evento.shiftKey) {
      evento.preventDefault();
      this.enviarFala();
    }
  }
}

function lerLarguraSalva(): number {
  try {
    const salva = Number(localStorage.getItem(CHAVE_LARGURA));
    return salva >= LARGURA_MINIMA ? salva : LARGURA_PADRAO;
  } catch {
    return LARGURA_PADRAO;
  }
}

function salvarLargura(px: number): void {
  try {
    localStorage.setItem(CHAVE_LARGURA, String(px));
  } catch {
    // Sem armazenamento (aba privada): vale só nesta visita.
  }
}

function lerEscolhaSalva(ferramenta: string): EscolhaDaIa {
  const padrao = escolhaPadrao(ferramenta);
  try {
    const salva = JSON.parse(localStorage.getItem(CHAVE_ESCOLHA + ferramenta) ?? 'null') as Partial<EscolhaDaIa> | null;
    return {
      modelo: MODELOS.find((m) => m.valor === salva?.modelo)?.valor ?? padrao.modelo,
      esforco: ESFORCOS.find((e) => e.valor === salva?.esforco)?.valor ?? padrao.esforco,
    };
  } catch {
    return padrao;
  }
}

function salvarEscolha(ferramenta: string, escolha: EscolhaDaIa): void {
  try {
    localStorage.setItem(CHAVE_ESCOLHA + ferramenta, JSON.stringify(escolha));
  } catch {
    // Sem armazenamento (aba privada): vale só nesta visita.
  }
}

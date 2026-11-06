import { ChangeDetectionStrategy, Component, ElementRef, afterRenderEffect, input, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AnexosImagemComponent } from '../anexos-imagem/anexos-imagem.component';
import { ImagemProtegidaComponent } from '../imagem-protegida.component';
import type { ImagemAnexada } from '../anexos-imagem/preparar-imagem';

/** Uma fala do chat. `acao` é o que a IA executou pelo MCP, mostrado como cartão com desfazer. */
export interface MensagemFerramenta {
  readonly id: number;
  readonly papel: 'joalheiro' | 'assistente';
  readonly texto: string;
  /** Imagens: caminho da API ou URL local (`data:`, recém-anexada). */
  readonly imagens?: readonly string[];
  readonly acao?: { readonly descricao: string; readonly desfeita: boolean };
}

/** O que o joalheiro manda ao assistente. */
export interface FalaFerramenta {
  readonly texto: string;
  readonly imagens: readonly ImagemAnexada[];
}

const LARGURA_PADRAO = 416;
const LARGURA_MINIMA = 288;
const CHAVE_LARGURA = 'castwork.ferramenta-ia.largura-chat';

/**
 * Tela padrão das ferramentas com IA: barra fina no topo (voltar, nome, ações e ⋯ com o histórico),
 * área de trabalho à esquerda com as propriedades embaixo, e o assistente à direita.
 * Cada ferramenta só preenche a área (`[area]`), as propriedades (`[propriedades]`) e as ações (`[acoes]`).
 * A IA aplica direto; cada ação vira um cartão no chat com Desfazer.
 */
@Component({
  selector: 'cw-ferramenta-ia',
  imports: [FormsModule, RouterLink, AnexosImagemComponent, ImagemProtegidaComponent],
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
              <button type="button" role="menuitem" (click)="abrirHistorico()"><i class="bi bi-clock-history" aria-hidden="true"></i> Histórico da conversa</button>
              <button type="button" role="menuitem" class="perigo" (click)="pedirApagar()"><i class="bi bi-trash3" aria-hidden="true"></i> Apagar conversa</button>
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
          <button type="button" class="btn btn-sutil btn-icone" aria-label="Apagar conversa" title="Apagar conversa" [disabled]="mensagens().length === 0" (click)="pedirApagar()">
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
            <span id="texto-apagar">Apagar toda a conversa? O desenho não muda.</span>
            <button type="button" class="btn btn-perigo-sutil" (click)="apagar()">Apagar</button>
            <button type="button" class="btn btn-sutil" (click)="confirmandoApagar.set(false)">Cancelar</button>
          </div>
        }
        <ol class="conversa" aria-live="polite">
          @for (m of mensagens(); track m.id) {
            <li class="fala" [class]="'fala-' + m.papel">
              @if (m.imagens?.length) {
                <div class="imagens">
                  @for (src of m.imagens; track src) {
                    <cw-imagem-protegida [src]="src" [rotulo]="m.papel === 'joalheiro' ? 'Imagem enviada' : 'O desenho como a IA viu'" />
                  }
                </div>
              }
              @if (m.texto) {
                <p>{{ m.texto }}</p>
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
            <li class="fala fala-assistente pensando"><p><span class="spinner-grow spinner-grow-sm" aria-hidden="true"></span> Trabalhando no desenho...</p></li>
          }
          <li #fim aria-hidden="true"></li>
        </ol>
        <form class="entrada" (ngSubmit)="enviarFala()" (dragover)="$event.preventDefault()" (drop)="soltar($event, anexador)">
          <cw-anexos-imagem #anexador class="anexos" [(imagens)]="anexos" />
          <button type="button" class="btn btn-sutil btn-icone" aria-label="Anexar imagem" title="Anexar imagem" [disabled]="pensando()" (click)="anexador.escolher()">
            <i class="bi bi-paperclip" aria-hidden="true"></i>
          </button>
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
          <button type="submit" class="btn btn-primary btn-icone" aria-label="Enviar" [disabled]="(!fala.trim() && anexos().length === 0) || pensando()">
            <i class="bi bi-arrow-up" aria-hidden="true"></i>
          </button>
        </form>
      </aside>
    </div>

    <dialog #historico class="historico" aria-labelledby="titulo-historico" (close)="historicoAberto.set(false)">
      <header>
        <h2 id="titulo-historico">Histórico da conversa</h2>
        <button type="button" class="btn btn-sutil btn-icone" aria-label="Fechar" (click)="historico.close()"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
      </header>
      @if (historicoAberto()) {
        <ol>
          @for (m of mensagens(); track m.id) {
            <li>
              <span class="quem">{{ m.papel === 'joalheiro' ? 'Você' : 'Assistente' }}</span>
              <span>{{ m.texto }}</span>
              @if (m.acao; as acao) {
                <span class="acao-historico">{{ acao.desfeita ? 'Desfeito' : 'Aplicou' }}: {{ acao.descricao }}</span>
              }
            </li>
          }
        </ol>
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

  readonly enviar = output<FalaFerramenta>();
  readonly desfazer = output<number>();
  readonly apagarConversa = output();

  protected readonly menuAberto = signal(false);
  protected readonly historicoAberto = signal(false);
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
  protected fala = '';

  private readonly historico = viewChild.required<ElementRef<HTMLDialogElement>>('historico');
  private readonly fim = viewChild.required<ElementRef<HTMLElement>>('fim');

  constructor() {
    afterRenderEffect(() => {
      this.mensagens();
      this.pensando();
      this.fim().nativeElement.scrollIntoView({ block: 'end' });
    });
  }

  protected abrirHistorico(): void {
    this.menuAberto.set(false);
    this.historicoAberto.set(true);
    this.historico().nativeElement.showModal();
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
    if ((!texto && this.anexos().length === 0) || this.pensando()) return;
    this.enviar.emit({ texto, imagens: this.anexos() });
    this.fala = '';
    this.anexos.set([]);
  }

  /** Entre o mínimo útil para ler o chat e 70% da tela, para a área de trabalho não sumir. */
  private definirLargura(px: number): void {
    this.largura.set(Math.round(Math.min(Math.max(px, LARGURA_MINIMA), window.innerWidth * 0.7)));
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

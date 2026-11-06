import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import type { ChatDaConversaDto, ConversaFerramentaDto, MensagemFerramentaDto } from '@data/assistente/dto/ferramenta.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { ToastService } from '@shared/services/toast.service';
import { ANDAMENTO_VAZIO, type AndamentoDoTurno, aplicarEvento } from '@shared/ui/ferramenta-ia/andamento-ia.component';
import { type FalaFerramenta, FerramentaIaComponent, type MensagemFerramenta } from '@shared/ui/ferramenta-ia/ferramenta-ia.component';
import { type Subscription, firstValueFrom } from 'rxjs';

/** Intervalo para consultar a conversa enquanto a IA trabalha num turno que esta tela não enviou. */
const INTERVALO_DE_CONSULTA_MS = 4000;

/**
 * Tela padrão de toda ferramenta com IA (da versão, como o desenho 2D, ou do projeto, como o briefing): a moldura (`cw-ferramenta-ia`) com a conversa já ligada à API.
 * Carrega o chat, envia (com imagens), acompanha o turno ao vivo, retoma o turno ao voltar para a tela,
 * novo chat, conversas anteriores, desfazer e apagar. A ferramenta só preenche a área de trabalho
 * (`[area]`) e as ações (`[acoes]`), e reage a `arquivoMudou` (a IA mudou ou desfez o arquivo).
 */
@Component({
  selector: 'cw-conversa-ia',
  imports: [FerramentaIaComponent],
  template: `
    <cw-ferramenta-ia
      [titulo]="titulo()"
      [voltarPara]="voltarPara()"
      [voltarRotulo]="voltarRotulo()"
      [mensagens]="mensagens()"
      [pensando]="pensando()"
      [andamento]="andamento()"
      [chats]="chats()"
      (enviar)="enviar($event)"
      (desfazer)="desfazer($event)"
      (apagarConversa)="apagar()"
      (novoChat)="novoChat()"
      (verChats)="verChats()"
      (abrirChat)="abrirChat($event)"
      (fecharChats)="chats.set(null)"
    >
      <ng-container acoes><ng-content select="[acoes]" /></ng-container>
      <ng-container area><ng-content select="[area]" /></ng-container>
    </cw-ferramenta-ia>
  `,
  styles: `
    :host {
      display: contents;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConversaIaComponent {
  private readonly ferramentas = inject(FerramentasService);
  private readonly toast = inject(ToastService);

  readonly projetoId = input.required<number>();
  /** Nulo nas ferramentas do projeto (o briefing). */
  readonly versaoId = input.required<number | null>();
  /** Código da ferramenta na API (`desenho-2d`). */
  readonly ferramenta = input.required<string>();
  readonly titulo = input.required<string>();
  readonly voltarPara = input.required<string>();
  readonly voltarRotulo = input('Voltar');
  /**
   * Chamado antes de cada mensagem: a ferramenta guarda o que foi feito à mão, para a IA trabalhar no
   * arquivo atual. Um erro aqui cancela o envio (e vira aviso).
   */
  readonly antesDeEnviar = input<(() => Promise<void>) | null>(null);
  /** Com o chat vazio, a IA abre a conversa sozinha (o briefing resume o formulário e pergunta o que falta). */
  readonly abrirSozinha = input(false);

  /** O arquivo da ferramenta mudou pela IA (ou por um Desfazer): a área de trabalho deve recarregá-lo. */
  readonly arquivoMudou = output();
  /** A IA terminou um turno (a ferramenta relê o que ela pode ter mudado, como os campos do briefing). */
  readonly respondeu = output();

  /** A IA está trabalhando (a ferramenta usa para avisar e bloquear o que conflita). */
  readonly pensando = signal(false);

  protected readonly conversa = signal<ConversaFerramentaDto>({ mensagens: [], arquivoId: null, processando: false, chat: 1, chatsAnteriores: 0 });
  protected readonly andamento = signal<AndamentoDoTurno | null>(null);
  protected readonly chats = signal<readonly ChatDaConversaDto[] | null>(null);
  /** Fala enviada, mostrada enquanto a IA trabalha. */
  private readonly pendente = signal<MensagemFerramenta | null>(null);
  private destruido = false;

  protected readonly mensagens = computed<readonly MensagemFerramenta[]>(() => {
    const lista = this.conversa().mensagens.map((m) => this.paraChat(m));
    const pendente = this.pendente();
    return pendente ? [...lista, pendente] : lista;
  });

  constructor() {
    // O turno da IA continua no servidor se o joalheiro sair: a resposta é lida quando ele voltar.
    inject(DestroyRef).onDestroy(() => (this.destruido = true));
    effect(() => {
      const [projetoId, versaoId, ferramenta] = [this.projetoId(), this.versaoId(), this.ferramenta()];
      untracked(() => void this.carregar(projetoId, versaoId, ferramenta));
    });
  }

  /** A ferramenta gravou o arquivo (salvar, restaurar): a versão nova não conta como mudança da IA. */
  registrarArquivo(arquivoId: number | null): void {
    this.conversa.update((c) => ({ ...c, arquivoId }));
  }

  protected async enviar({ texto, imagens }: FalaFerramenta): Promise<void> {
    this.pensando.set(true);
    let aoVivo: Subscription | undefined;
    // A abertura (sem texto nem imagem) não aparece como fala no chat.
    this.pendente.set(texto || imagens.length > 0 ? { id: -1, papel: 'joalheiro', texto, imagens: imagens.map((i) => i.url) } : null);
    try {
      await this.antesDeEnviar()?.();
      const anterior = this.conversa().arquivoId;
      aoVivo = this.acompanhar(true);
      const conversa = await firstValueFrom(
        this.ferramentas.postConversa(this.projetoId(), this.versaoId(), this.ferramenta(), texto || null, imagens.map((i) => i.base64)),
      );
      if (this.destruido) return;
      this.conversa.set(conversa);
      this.respondeu.emit();
      if (conversa.arquivoId !== anterior) this.arquivoMudou.emit();
    } catch (erro) {
      // Erros da API já viram toast no interceptor; os da ferramenta (guardar o arquivo) aparecem aqui.
      if (!(erro instanceof HttpErrorResponse)) {
        this.toast.erro(erro instanceof Error ? erro.message : 'Não foi possível enviar a mensagem.');
      }
    } finally {
      aoVivo?.unsubscribe();
      this.pendente.set(null);
      this.andamento.set(null);
      this.pensando.set(false);
    }
  }

  protected async novoChat(): Promise<void> {
    this.conversa.set(await firstValueFrom(this.ferramentas.postNovoChat(this.projetoId(), this.versaoId(), this.ferramenta())));
    if (this.abrirSozinha()) await this.abrir();
  }

  protected async verChats(): Promise<void> {
    this.chats.set(await firstValueFrom(this.ferramentas.getChats(this.projetoId(), this.versaoId(), this.ferramenta())));
  }

  /** O chat escolhido no histórico vira o em andamento e aparece na conversa. */
  protected async abrirChat(numero: number): Promise<void> {
    if (numero !== this.conversa().chat) {
      this.conversa.set(await firstValueFrom(this.ferramentas.postContinuarChat(this.projetoId(), this.versaoId(), this.ferramenta(), numero)));
    }

    this.chats.set(null);
  }

  protected async desfazer(ordem: number): Promise<void> {
    this.conversa.set(await firstValueFrom(this.ferramentas.postDesfazer(this.projetoId(), this.versaoId(), this.ferramenta(), ordem)));
    this.arquivoMudou.emit();
  }

  protected async apagar(): Promise<void> {
    await firstValueFrom(this.ferramentas.deleteConversa(this.projetoId(), this.versaoId(), this.ferramenta()));
    this.conversa.update((c) => ({ ...c, mensagens: [] }));
  }

  private async carregar(projetoId: number, versaoId: number | null, ferramenta: string): Promise<void> {
    const conversa = await firstValueFrom(this.ferramentas.getConversa(projetoId, versaoId, ferramenta));
    this.conversa.set(conversa);
    if (conversa.processando) await this.aguardarIa();
    else if (conversa.mensagens.length === 0 && this.abrirSozinha()) await this.enviar({ texto: '', imagens: [] });
  }

  /** Abre a conversa pela IA quando o chat está vazio (depois do formulário, ou num chat novo). */
  async abrir(): Promise<void> {
    if (!this.pensando() && this.conversa().mensagens.length === 0) await this.enviar({ texto: '', imagens: [] });
  }

  /**
   * A fala já está no servidor e a IA trabalha nela (enviada antes de sair da tela): acompanha ao vivo e
   * consulta até a resposta chegar.
   */
  private async aguardarIa(): Promise<void> {
    this.pensando.set(true);
    const anterior = this.conversa().arquivoId;
    const aoVivo = this.acompanhar(false);
    try {
      while (!this.destruido) {
        await new Promise((ok) => setTimeout(ok, INTERVALO_DE_CONSULTA_MS));
        if (this.destruido) return;
        const conversa = await firstValueFrom(this.ferramentas.getConversa(this.projetoId(), this.versaoId(), this.ferramenta()));
        this.conversa.set(conversa);
        if (!conversa.processando) {
          this.respondeu.emit();
          if (conversa.arquivoId !== anterior) this.arquivoMudou.emit();
          return;
        }
      }
    } finally {
      aoVivo.unsubscribe();
      this.andamento.set(null);
      this.pensando.set(false);
    }
  }

  /** Abre o acompanhamento ao vivo do turno e soma cada evento ao andamento mostrado no chat. */
  private acompanhar(aguardar: boolean): Subscription {
    this.andamento.set(ANDAMENTO_VAZIO());
    return this.ferramentas.acompanhar(this.projetoId(), this.versaoId(), this.ferramenta(), aguardar).subscribe({
      next: (evento) => this.andamento.update((a) => (a ? aplicarEvento(a, evento) : a)),
      // Sem o ao vivo, o chat continua com "Trabalhando..." e a resposta chega do mesmo jeito.
      error: () => undefined,
    });
  }

  private paraChat(m: MensagemFerramentaDto): MensagemFerramenta {
    return {
      id: m.ordem,
      papel: m.papel === 'Joalheiro' ? 'joalheiro' : 'assistente',
      texto: m.texto ?? '',
      imagens: m.imagens.map((chave) => this.ferramentas.caminhoDaImagem(this.projetoId(), this.versaoId(), chave)),
      acao: m.acao ?? undefined,
      pensamento: m.pensamento,
      passos: m.passos,
      tokens: m.tokens,
    };
  }
}

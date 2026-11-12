import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import type { ChatDaConversaDto, ConversaFerramentaDto, MensagemFerramentaDto } from '@data/assistente/dto/ferramenta.dto';
import { type EscopoDaFerramenta, FerramentasService } from '@data/assistente/ferramentas.service';
import { ToastService } from '@shared/services/toast.service';
import { ANDAMENTO_VAZIO, type AndamentoDoTurno, aplicarEvento } from '@shared/ui/ferramenta-ia/andamento-ia.component';
import type { ArquivoDoProjeto } from '@shared/ui/ferramenta-ia/arquivos-do-projeto.component';
import { type FalaFerramenta, FerramentaIaComponent, type MensagemFerramenta } from '@shared/ui/ferramenta-ia/ferramenta-ia.component';
import { type Subscription, firstValueFrom } from 'rxjs';

/** Intervalo para consultar a conversa enquanto a IA trabalha num turno que esta tela não enviou. */
const INTERVALO_DE_CONSULTA_MS = 4000;
/** Sem resposta do servidor (rede caiu, proxy desistiu do pedido longo): o turno pode estar correndo lá. */
const STATUS_DE_CONEXAO: readonly number[] = [0, 502, 503, 504];

/**
 * Tela padrão de toda ferramenta com IA (da versão, como o desenho 2D, ou do projeto, como o briefing): a moldura (`cw-ferramenta-ia`) com a conversa já ligada à API.
 * Carrega o chat, envia (com imagens e arquivos do projeto), acompanha o turno ao vivo, retoma o turno ao voltar para a tela,
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
      [listarArquivosDoProjeto]="listarArquivosDoProjeto"
      [boasVindas]="boasVindas()"
      [ferramenta]="ferramenta()"
      [chatUnico]="daPeca()"
      [somenteLeitura]="somenteLeitura()"
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
  /** A versão (Id), a peça do projeto (`{ pecaId }`) ou, nas ferramentas do projeto (o briefing), nulo. */
  readonly escopo = input.required<EscopoDaFerramenta>();
  /** Código da ferramenta na API (`desenho-2d`). */
  readonly ferramenta = input.required<string>();
  readonly titulo = input.required<string>();
  readonly voltarPara = input.required<string>();
  readonly voltarRotulo = input('Voltar');
  /** Sem `projetos.editar`: a conversa é só para consulta (a API recusaria qualquer envio com 403). */
  readonly somenteLeitura = input(false);
  /**
   * Chamado antes de cada mensagem: a ferramenta guarda o que foi feito à mão, para a IA trabalhar no
   * arquivo atual. Um erro aqui cancela o envio (e vira aviso).
   */
  readonly antesDeEnviar = input<(() => Promise<void>) | null>(null);
  /** Mensagem fixa no chat vazio, no lugar de a IA abrir a conversa (que custa uma chamada). */
  readonly boasVindas = input<string | null>(null);

  /** O arquivo da ferramenta mudou pela IA (ou por um Desfazer): a área de trabalho deve recarregá-lo. */
  readonly arquivoMudou = output();
  /** A IA terminou um turno (a ferramenta relê o que ela pode ter mudado, como os campos do briefing). */
  readonly respondeu = output();

  /** A IA está trabalhando (a ferramenta usa para avisar e bloquear o que conflita). */
  readonly pensando = signal(false);

  /** Na peça do projeto, uma conversa só por ferramenta (sem chats paralelos). */
  protected readonly daPeca = computed(() => {
    const escopo = this.escopo();
    return typeof escopo === 'object' && escopo !== null;
  });
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
      const [projetoId, escopo, ferramenta] = [this.projetoId(), this.escopo(), this.ferramenta()];
      untracked(() => void this.carregar(projetoId, escopo, ferramenta));
    });
  }

  /** A ferramenta gravou o arquivo (salvar, restaurar): a versão nova não conta como mudança da IA. */
  registrarArquivo(arquivoId: number | null): void {
    this.conversa.update((c) => ({ ...c, arquivoId }));
  }

  /** "Do projeto": o atual de cada ferramenta (de todas as versões) e o último PDF do briefing. */
  protected readonly listarArquivosDoProjeto = (): Promise<readonly ArquivoDoProjeto[]> =>
    firstValueFrom(this.ferramentas.getAnexosDoProjeto(this.projetoId()));

  protected async enviar({ texto, imagens, arquivos, modelo, esforco }: FalaFerramenta): Promise<void> {
    if (this.somenteLeitura()) return;
    this.pensando.set(true);
    let aoVivo: Subscription | undefined;
    // A abertura (sem texto nem imagem) não aparece como fala no chat.
    this.pendente.set(
      texto || imagens.length > 0 || arquivos.length > 0
        ? { id: -1, papel: 'joalheiro', texto, imagens: imagens.map((i) => i.url), anexos: arquivos.map((a) => a.nome) }
        : null,
    );
    let anterior = this.conversa().arquivoId;
    let enviada = false;
    let retomar = false;
    try {
      await this.antesDeEnviar()?.();
      anterior = this.conversa().arquivoId;
      aoVivo = this.acompanhar(true);
      enviada = true;
      const conversa = await firstValueFrom(
        this.ferramentas.postConversa(
          this.projetoId(),
          this.escopo(),
          this.ferramenta(),
          texto || null,
          imagens.map((i) => i.base64),
          arquivos.map((a) => ({ origem: a.origem, id: a.id })),
          { modelo, esforco },
        ),
      );
      if (this.destruido) return;
      this.conversa.set(conversa);
      this.respondeu.emit();
      if (conversa.arquivoId !== anterior) this.arquivoMudou.emit();
    } catch (erro) {
      // A conexão caiu no meio do pedido longo: a fala pode ter chegado e a IA seguir trabalhando no servidor.
      // Em vez de dar o turno por perdido, a tela passa a consultar até saber o que aconteceu.
      retomar = enviada && erro instanceof HttpErrorResponse && STATUS_DE_CONEXAO.includes(erro.status);
      // Erros da API já viram toast no interceptor; os da ferramenta (guardar o arquivo) aparecem aqui.
      if (!(erro instanceof HttpErrorResponse)) {
        this.toast.erro(erro instanceof Error ? erro.message : 'Não foi possível enviar a mensagem.');
      }
    } finally {
      aoVivo?.unsubscribe();
      if (!retomar) {
        this.pendente.set(null);
        this.andamento.set(null);
        this.pensando.set(false);
      }
    }
    if (retomar && !this.destruido) await this.aguardarIa(anterior);
  }

  /** Uma ação do chat por vez, e nenhuma com a IA trabalhando (apagar ou trocar de chat no meio do turno o perderia). */
  private agindo = false;

  private async acao(executar: () => Promise<void>): Promise<void> {
    if (this.agindo || this.pensando() || this.somenteLeitura()) return;
    this.agindo = true;
    try {
      await executar();
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível concluir a ação.');
    } finally {
      this.agindo = false;
    }
  }

  protected novoChat(): Promise<void> {
    return this.acao(async () => this.conversa.set(await firstValueFrom(this.ferramentas.postNovoChat(this.projetoId(), this.escopo(), this.ferramenta()))));
  }

  protected async verChats(): Promise<void> {
    try {
      this.chats.set(await firstValueFrom(this.ferramentas.getChats(this.projetoId(), this.escopo(), this.ferramenta())));
    } catch {
      // O interceptor já avisou.
    }
  }

  /** O chat escolhido no histórico vira o em andamento e aparece na conversa. */
  protected abrirChat(numero: number): Promise<void> {
    return this.acao(async () => {
      if (numero !== this.conversa().chat) {
        this.conversa.set(await firstValueFrom(this.ferramentas.postContinuarChat(this.projetoId(), this.escopo(), this.ferramenta(), numero)));
      }
      this.chats.set(null);
    });
  }

  /** Um Desfazer por vez: o segundo clique esperaria um arquivo que o primeiro já trocou. */
  protected desfazer(ordem: number): Promise<void> {
    return this.acao(async () => {
      this.conversa.set(await firstValueFrom(this.ferramentas.postDesfazer(this.projetoId(), this.escopo(), this.ferramenta(), ordem)));
      this.arquivoMudou.emit();
    });
  }

  protected apagar(): Promise<void> {
    return this.acao(async () => {
      await firstValueFrom(this.ferramentas.deleteConversa(this.projetoId(), this.escopo(), this.ferramenta()));
      this.conversa.update((c) => ({ ...c, mensagens: [] }));
    });
  }

  private async carregar(projetoId: number, escopo: EscopoDaFerramenta, ferramenta: string): Promise<void> {
    let conversa: ConversaFerramentaDto;
    try {
      conversa = await firstValueFrom(this.ferramentas.getConversa(projetoId, escopo, ferramenta));
    } catch {
      // O interceptor já avisou; o chat fica vazio e a próxima mensagem funciona normalmente.
      return;
    }
    // A rota trocou de versão ou peça enquanto esta resposta vinha: ela não é mais a conversa à vista.
    if (projetoId !== this.projetoId() || escopo !== this.escopo() || ferramenta !== this.ferramenta()) return;
    this.conversa.set(conversa);
    if (conversa.processando) await this.aguardarIa();

  }

  /**
   * A fala já está no servidor e a IA trabalha nela (enviada antes de sair da tela): acompanha ao vivo e
   * consulta até a resposta chegar.
   */
  private async aguardarIa(anterior = this.conversa().arquivoId): Promise<void> {
    this.pensando.set(true);
    const aoVivo = this.acompanhar(false);
    try {
      while (!this.destruido) {
        await new Promise((ok) => setTimeout(ok, INTERVALO_DE_CONSULTA_MS));
        if (this.destruido) return;
        let conversa: ConversaFerramentaDto;
        try {
          conversa = await firstValueFrom(this.ferramentas.getConversa(this.projetoId(), this.escopo(), this.ferramenta(), true));
        } catch {
          // Uma consulta que falha (rede instável) não encerra o acompanhamento: tenta de novo no próximo intervalo.
          continue;
        }
        this.conversa.set(conversa);
        if (!conversa.processando) {
          this.respondeu.emit();
          if (conversa.arquivoId !== anterior) this.arquivoMudou.emit();
          return;
        }
      }
    } finally {
      aoVivo.unsubscribe();
      this.pendente.set(null);
      this.andamento.set(null);
      this.pensando.set(false);
    }
  }

  /** Abre o acompanhamento ao vivo do turno e soma cada evento ao andamento mostrado no chat. */
  private acompanhar(aguardar: boolean): Subscription {
    this.andamento.set(ANDAMENTO_VAZIO());
    return this.ferramentas.acompanhar(this.projetoId(), this.escopo(), this.ferramenta(), aguardar).subscribe({
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
      imagens: m.imagens.map((chave) => this.ferramentas.caminhoDaImagem(this.projetoId(), this.escopo(), chave)),
      acao: m.acao ?? undefined,
      pensamento: m.pensamento,
      passos: m.passos,
      tokens: m.tokens,
      anexos: m.anexos?.map((a) => a.nome),
    };
  }
}

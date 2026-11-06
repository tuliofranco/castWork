import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';

/** Um passo do turno: uma ferramenta que a IA usou, com o render quando ela olhou o desenho. */
export interface PassoAoVivo {
  readonly numero: number;
  readonly descricao: string;
  readonly estado: 'andamento' | 'feito' | 'erro';
  /** data: URL do render (PNG). */
  readonly imagem?: string;
}

/** O turno da IA enquanto acontece: o que ela pensa, os passos e o texto da resposta sendo escrito. */
export interface AndamentoDoTurno {
  readonly inicio: number;
  readonly pensamento: string;
  readonly texto: string;
  readonly passos: readonly PassoAoVivo[];
}

/** O mesmo formato de evento que a API manda (`EventoDoTurnoDto`), sem prender o componente à camada de dados. */
interface EventoDoTurno {
  readonly tipo: string;
  readonly texto?: string | null;
  readonly passo?: number | null;
  readonly estado?: string | null;
  readonly imagemPng?: string | null;
}

export const ANDAMENTO_VAZIO = (): AndamentoDoTurno => ({ inicio: Date.now(), pensamento: '', texto: '', passos: [] });

/** Soma um evento ao andamento. Puro: devolve um novo andamento (para o signal notificar). */
export function aplicarEvento(andamento: AndamentoDoTurno, evento: EventoDoTurno): AndamentoDoTurno {
  switch (evento.tipo) {
    case 'pensamento':
      return { ...andamento, pensamento: andamento.pensamento + (evento.texto ?? '') };
    case 'texto':
      return { ...andamento, texto: andamento.texto + (evento.texto ?? '') };
    case 'passo': {
      const numero = evento.passo ?? 0;
      const estado = (evento.estado ?? 'andamento') as PassoAoVivo['estado'];
      const imagem = evento.imagemPng ? `data:image/png;base64,${evento.imagemPng}` : undefined;
      const existente = andamento.passos.find((p) => p.numero === numero);
      const passos = existente
        ? andamento.passos.map((p) => (p.numero === numero ? { ...p, estado, imagem: imagem ?? p.imagem } : p))
        : [...andamento.passos, { numero, descricao: evento.texto ?? 'Trabalhando no desenho', estado, imagem }];
      return { ...andamento, passos };
    }
    default:
      return andamento;
  }
}

/**
 * O andamento ao vivo no chat, no lugar da resposta: tempo de trabalho, o pensamento (recolhido), os passos
 * e o texto da resposta aparecendo aos poucos. Passos iguais em sequência aparecem uma vez, com a contagem.
 */
@Component({
  selector: 'cw-andamento-ia',
  template: `
    <div class="andamento" role="status" aria-live="polite">
      <p class="cabeca"><span class="pulso" aria-hidden="true"></span> Trabalhando · {{ tempo() }}</p>
      @if (andamento().pensamento) {
        <details class="pensamento">
          <summary>Pensando…</summary>
          <p>{{ andamento().pensamento }}</p>
        </details>
      }
      @if (grupos().length > 0) {
        <ol class="passos">
          @for (g of grupos(); track g.chave) {
            <li [class]="'passo-' + g.estado">
              <i class="bi" [class]="icone(g.estado)" aria-hidden="true"></i>
              <span>{{ g.descricao }}@if (g.vezes > 1) {<small> ×{{ g.vezes }}</small>}</span>
              @if (g.imagem) {
                <img [src]="g.imagem" alt="O desenho como a IA viu" />
              }
            </li>
          }
        </ol>
      }
      @if (andamento().texto) {
        <p class="texto">{{ andamento().texto }}</p>
      }
    </div>
  `,
  styleUrl: './andamento-ia.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AndamentoIaComponent {
  readonly andamento = input.required<AndamentoDoTurno>();

  private readonly agora = signal(Date.now());

  protected readonly tempo = computed(() => {
    const segundos = Math.max(0, Math.round((this.agora() - this.andamento().inicio) / 1000));
    return `${Math.floor(segundos / 60)}:${String(segundos % 60).padStart(2, '0')}`;
  });

  /** Passos iguais em sequência viram um grupo: "Desenhando arcos ×4". */
  protected readonly grupos = computed(() => {
    const grupos: { chave: number; descricao: string; estado: PassoAoVivo['estado']; vezes: number; imagem?: string }[] = [];
    for (const passo of this.andamento().passos) {
      const ultimo = grupos.at(-1);
      if (ultimo?.descricao === passo.descricao) {
        ultimo.vezes++;
        ultimo.estado = passo.estado === 'erro' ? ultimo.estado : passo.estado;
        ultimo.imagem = passo.imagem ?? ultimo.imagem;
      } else {
        grupos.push({ chave: passo.numero, descricao: passo.descricao, estado: passo.estado, vezes: 1, imagem: passo.imagem });
      }
    }
    return grupos;
  });

  constructor() {
    const relogio = setInterval(() => this.agora.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(relogio));
  }

  protected icone(estado: PassoAoVivo['estado']): string {
    const icones: Record<PassoAoVivo['estado'], string> = { feito: 'bi-check2', erro: 'bi-exclamation-circle', andamento: 'bi-arrow-repeat girando' };
    return icones[estado];
  }
}

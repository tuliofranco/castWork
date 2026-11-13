import { ChangeDetectionStrategy, Component, forwardRef, input, output, signal } from '@angular/core';

import { ConversaIaComponent } from '../conversa-ia.component';

/**
 * A conversa das ferramentas, só para os specs delas: a de verdade fala com a API; aqui fica o que o Desenho 2D e o
 * Modelo 3D usam (o `pensando`, o `registrarArquivo` e o aviso de que a IA mudou o arquivo).
 */
@Component({
  selector: 'cw-conversa-ia',
  template: '<ng-content select="[acoes]" /><ng-content select="[area]" />',
  providers: [{ provide: ConversaIaComponent, useExisting: forwardRef(() => ConversaFalsaComponent) }],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConversaFalsaComponent {
  readonly ferramenta = input<string>();
  readonly titulo = input<string>();
  readonly projetoId = input<number>();
  readonly escopo = input<unknown>();
  readonly voltarPara = input<string>();
  readonly voltarRotulo = input<string>();
  readonly antesDeEnviar = input<(() => Promise<void>) | null>(null);
  readonly somenteLeitura = input(false);
  readonly arquivoMudou = output();
  readonly pensando = signal(false);

  registrarArquivo(): void {
    // A conversa de verdade guarda o Id; os specs das ferramentas não olham para ele.
  }
}

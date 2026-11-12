import { HttpErrorResponse } from '@angular/common/http';
import type { FormGroup } from '@angular/forms';

/**
 * 400 da API (ValidationProblemDetails) → erro no campo correspondente do formulário, com a mensagem do servidor.
 * As chaves vêm em camelCase, iguais aos nomes dos controles. Devolve as mensagens que não casaram com nenhum campo.
 */
export function aplicarErrosDoServidor(form: FormGroup, erro: unknown): string[] {
  if (!(erro instanceof HttpErrorResponse) || erro.status !== 400) return [];
  const errors = (erro.error as { errors?: Record<string, readonly string[]> } | null)?.errors ?? {};

  const semCampo: string[] = [];
  for (const [campo, mensagens] of Object.entries(errors)) {
    // "Itens[0].Quantidade" (coleção) vira o caminho que o Angular entende: "Itens.0.Quantidade".
    const controle = form.get(campo.replace(/\[(\d+)\]/g, '.$1'));
    if (controle && mensagens.length > 0) {
      controle.setErrors({ servidor: mensagens[0] });
      controle.markAsTouched();
    } else {
      semCampo.push(...mensagens);
    }
  }
  return semCampo;
}

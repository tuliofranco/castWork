import type { ModeloMontadoDto } from '@data/ferramentas/modelo-3d/dto/modelo-montado.dto';

/**
 * O aviso depois do "Montar a partir das peças": quantas unidades entraram e quais peças ficaram de fora por não terem
 * modelo 3D final ("4 unidades de 2 peças trazidas para o modelo. Sem modelo 3D final: Fecho.").
 */
export function resumoDaMontagem(montado: ModeloMontadoDto): string {
  const unidades = montado.pecas.reduce((total, p) => total + p.quantidade, 0);
  const pecas = montado.pecas.length;
  const trazidas = `${unidades} ${unidades === 1 ? 'unidade' : 'unidades'} de ${pecas} ${pecas === 1 ? 'peça trazida' : 'peças trazidas'} para o modelo.`;
  return montado.pecasSemModelo.length === 0 ? trazidas : `${trazidas} Sem modelo 3D final: ${montado.pecasSemModelo.join(', ')}.`;
}

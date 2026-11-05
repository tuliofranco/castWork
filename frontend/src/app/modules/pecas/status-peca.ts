import type { StatusPeca } from '@data/pecas/pecas/dto/peca-resumo.dto';

/** Rótulo e cor (classe do Bootstrap) de cada etapa do pipeline. */
export const STATUS_PECA: Readonly<Record<StatusPeca, { readonly rotulo: string; readonly classe: string }>> = {
  BRIEFING: { rotulo: 'Briefing', classe: 'text-bg-secondary' },
  ORCAMENTO: { rotulo: 'Orçamento', classe: 'text-bg-info' },
  APROVADO: { rotulo: 'Aprovado', classe: 'text-bg-primary' },
  PRODUCAO: { rotulo: 'Produção', classe: 'text-bg-warning' },
  ACABAMENTO: { rotulo: 'Acabamento', classe: 'text-bg-warning' },
  ENTREGUE: { rotulo: 'Entregue', classe: 'text-bg-success' },
  POS_VENDA: { rotulo: 'Pós-venda', classe: 'text-bg-success' },
  CANCELADA: { rotulo: 'Cancelada', classe: 'text-bg-dark' },
};

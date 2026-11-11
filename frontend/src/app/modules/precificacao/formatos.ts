import type { Metal } from '@data/precificacao/precos/dto/precos.dto';

/** Acima disso a cotação pede atualização (decisão do modelo em docs/PRECIFICACAO.md). */
export const DIAS_PARA_DESATUALIZAR = 15;

const DIA_MS = 24 * 60 * 60 * 1000;

export const METAIS: readonly { readonly valor: Metal; readonly rotulo: string; readonly puro: string }[] = [
  { valor: 'OURO', rotulo: 'Ouro', puro: 'ouro puro (24k)' },
  { valor: 'PRATA', rotulo: 'Prata', puro: 'prata fina' },
];

export function rotuloMetal(metal: Metal): string {
  return METAIS.find((m) => m.valor === metal)?.rotulo ?? metal;
}

export function metalPuro(metal: Metal): string {
  return METAIS.find((m) => m.valor === metal)?.puro ?? metal;
}

/** 612.35 → "R$ 612,35". */
export function moeda(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

/** 10.36 → "10,36". */
export function numero(valor: number): string {
  return valor.toLocaleString('pt-BR', { maximumFractionDigits: 3 });
}

/** Dias inteiros desde a data (0 no mesmo dia). */
export function diasDesde(data: string, agora = Date.now()): number {
  return Math.max(0, Math.floor((agora - new Date(data).getTime()) / DIA_MS));
}

/** "hoje", "há 1 dia", "há 16 dias". */
export function haQuanto(dias: number): string {
  if (dias === 0) return 'hoje';
  return dias === 1 ? 'há 1 dia' : `há ${dias} dias`;
}

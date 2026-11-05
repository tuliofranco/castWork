/** Valores do enum TipoPeca como chegam do GraphQL. */
export type TipoPeca = 'ELO' | 'FECHO' | 'PEDRA' | 'PINGENTE' | 'CORRENTE' | 'ENGASTE' | 'OUTRO';

export const TIPOS_PECA: readonly { readonly valor: TipoPeca; readonly rotulo: string; readonly icone: string }[] = [
  { valor: 'ELO', rotulo: 'Elo', icone: 'link-45deg' },
  { valor: 'FECHO', rotulo: 'Fecho', icone: 'lock' },
  { valor: 'PEDRA', rotulo: 'Pedra', icone: 'gem' },
  { valor: 'PINGENTE', rotulo: 'Pingente', icone: 'droplet' },
  { valor: 'CORRENTE', rotulo: 'Corrente', icone: 'link' },
  { valor: 'ENGASTE', rotulo: 'Engaste', icone: 'bullseye' },
  { valor: 'OUTRO', rotulo: 'Outro', icone: 'three-dots' },
];

export function rotuloTipoPeca(tipo: TipoPeca): string {
  return TIPOS_PECA.find((t) => t.valor === tipo)?.rotulo ?? tipo;
}

export function iconeTipoPeca(tipo: TipoPeca): string {
  return TIPOS_PECA.find((t) => t.valor === tipo)?.icone ?? 'circle';
}

/** 750 → "750 (18k)". Quilates com uma casa só quando não for inteiro. */
export function formatarTeor(milesimos: number | null): string {
  if (milesimos === null) return '—';
  const quilates = Math.round((milesimos / 1000) * 24 * 10) / 10;
  return `${milesimos} (${Number.isInteger(quilates) ? quilates : quilates.toFixed(1)}k)`;
}

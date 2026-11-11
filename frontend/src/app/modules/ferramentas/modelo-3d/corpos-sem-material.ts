/** Um corpo como o `inspect.measure` do SolidCraft devolve (só o que importa aqui). */
interface CorpoMedido {
  readonly name?: string;
  readonly material?: string;
}

/** O material que o SolidCraft dá a todo corpo (1,29 g/cm³, plástico): para o Castwork, corpo sem material. */
const MATERIAL_PADRAO_DO_SOLIDCRAFT = 'Default';

/**
 * Os corpos sem material a partir do `inspect.measure` do SolidCraft: os que ficaram com o "Default" (ou sem
 * nenhum). Uma resposta que não é a medida esperada não aponta nada.
 */
export function corposSemMaterial(medida: unknown): string[] {
  const corpos = (medida as { bodies?: unknown } | null)?.bodies;
  if (!Array.isArray(corpos)) return [];
  return (corpos as CorpoMedido[])
    .filter((corpo) => !corpo.material || corpo.material === MATERIAL_PADRAO_DO_SOLIDCRAFT)
    .map((corpo, indice) => corpo.name ?? `Corpo ${indice + 1}`);
}

/** "1 corpo sem material", "2 corpos sem material". */
export function avisoDeCorposSemMaterial(quantidade: number): string {
  return quantidade === 1 ? '1 corpo sem material' : `${quantidade} corpos sem material`;
}

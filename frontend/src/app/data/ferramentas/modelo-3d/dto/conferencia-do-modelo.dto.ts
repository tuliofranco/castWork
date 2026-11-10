/** Para que serve o corpo, pelo material dele no arquivo 3D (valores do enum como chegam do GraphQL). */
export type UsoDoCorpo = 'METAL' | 'PEDRA' | 'APOIO' | 'SEM_MATERIAL';

/** Um corpo do modelo: material, volume e peso (nulos em corpo aberto). */
export interface CorpoConferidoDto {
  readonly nome: string;
  /** O material no arquivo (`Castwork liga 2`, `Pedra`, `Default`...); nulo sem nenhum. */
  readonly material: string | null;
  readonly uso: UsoDoCorpo;
  readonly ligaId: number | null;
  /** O nome da liga, nos corpos de metal. */
  readonly liga: string | null;
  readonly volumeMm3: number | null;
  readonly massaGramas: number | null;
  readonly fechado: boolean;
  /** Entra no peso do metal (corpo fechado de uma liga). */
  readonly contaNoPeso: boolean;
}

export interface SobreposicaoConferidaDto {
  readonly corpoA: string;
  readonly corpoB: string;
  readonly volumeMm3: number;
  /** Os dois são de metal: a parte comum conta duas vezes no peso. */
  readonly deMetal: boolean;
  /** Os dois são da mesma liga e podem virar um corpo só. */
  readonly podeUnir: boolean;
}

/** Um material que se pode escolher: liga ativa do ateliê, Pedra ou Apoio. */
export interface MaterialDisponivelDto {
  /** O nome no arquivo, que vai no command. */
  readonly nome: string;
  /** Como aparece na tela ("Ouro 18k amarelo"). */
  readonly rotulo: string;
  readonly uso: UsoDoCorpo;
  readonly ligaId: number | null;
}

export interface PesoDaLigaDto {
  readonly ligaId: number;
  readonly liga: string;
  readonly peso: number;
}

/**
 * Conferência de peso e materiais do modelo 3D da versão. `arquivoId` nulo: a versão não tem 3D; `medido` falso: não
 * foi possível medir. `alteravel` falso (publicada ou arquivada): só leitura.
 */
export interface ConferenciaDoModeloDto {
  readonly arquivoId: number | null;
  readonly medido: boolean;
  readonly alteravel: boolean;
  readonly pesoDoMetal: number;
  readonly pesosPorLiga: readonly PesoDaLigaDto[];
  readonly corpos: readonly CorpoConferidoDto[];
  readonly sobreposicoes: readonly SobreposicaoConferidaDto[];
  readonly materiais: readonly MaterialDisponivelDto[];
}

/** O material de um corpo, pelo nome do material no arquivo. */
export interface MaterialDoCorpoDto {
  readonly corpo: string;
  readonly material: string;
}

export interface PutMateriaisDosCorposDto {
  /** O arquivo que a conferência mostrou: se o modelo mudou depois, o servidor recusa. */
  readonly arquivoId: number;
  readonly corpos: readonly MaterialDoCorpoDto[];
}

export interface PostUnirCorposDto {
  readonly arquivoId: number;
  /** O primeiro dá o nome ao corpo unido. */
  readonly corpos: readonly string[];
}

/** O arquivo que a conferência gravou e o de antes (que o Desfazer restaura). */
export interface ModeloAlteradoDto {
  readonly arquivoId: number;
  readonly arquivoAnteriorId: number;
}

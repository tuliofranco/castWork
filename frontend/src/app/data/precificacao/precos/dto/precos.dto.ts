/** Valores do enum Metal como chegam do GraphQL. */
export type Metal = 'OURO' | 'PRATA';

/** Uma linha do histórico de cotações (R$/g do metal puro). */
export interface CotacaoDto {
  readonly valor: number;
  readonly registradaEm: string;
  readonly registradaPor: string | null;
}

export interface CotacaoVigenteDto {
  readonly metal: Metal;
  /** Nula enquanto o ateliê não registrar nenhuma. */
  readonly atual: CotacaoDto | null;
}

export interface LigaDto {
  readonly id: number;
  readonly nome: string;
  readonly metal: Metal;
  /** Milésimos (750 = 18k). */
  readonly teor: number;
  /** g/cm³. */
  readonly densidade: number;
}

export interface ParametrosDePrecoDto {
  readonly perdaPercentual: number;
  /** Zero enquanto o ateliê não definir. */
  readonly valorHora: number;
  readonly markup: number;
  readonly taxasPercentual: number;
  readonly arredondarPara: number;
}

export interface PrecosDoAtelieDto {
  readonly cotacoes: readonly CotacaoVigenteDto[];
  readonly ligas: readonly LigaDto[];
  readonly parametros: ParametrosDePrecoDto;
}

export interface GetHistoricoDeCotacoesQuery {
  readonly metal: Metal;
  readonly primeiros?: number;
  readonly apos?: string | null;
}

/** O backend lê o enum pelo nome, sem diferenciar maiúsculas. */
export interface PostCotacaoDto {
  readonly metal: Metal;
  readonly valor: number;
}

export interface PostLigaDto {
  readonly nome: string;
  readonly metal: Metal;
  /** "18k" ou "750". */
  readonly teor: string;
  readonly densidade: number;
}

export type PutLigaDto = PostLigaDto;

export type PutParametrosDePrecoDto = ParametrosDePrecoDto;

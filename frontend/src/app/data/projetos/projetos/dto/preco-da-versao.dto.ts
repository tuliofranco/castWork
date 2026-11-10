import type { Metal } from '@data/precificacao/precos/dto/precos.dto';

export type StatusDoPreco = 'COMPLETO' | 'INCOMPLETO';

/** Valores do enum como chegam do GraphQL. */
export type TipoMaoDeObra = 'POR_HORAS' | 'VALOR_FIXO';

export type TipoDePendencia =
  | 'PECA_SEM_CUSTO'
  | 'METAL_SEM_COTACAO'
  | 'SEM_METAL_COM_PESO'
  | 'LIGA_DESATIVADA'
  | 'VALOR_HORA_A_DEFINIR'
  | 'CORPOS_SEM_MATERIAL'
  | 'MODELO_NAO_MEDIDO';

export type TipoDeAviso = 'COTACAO_ANTIGA' | 'CORPOS_SOBREPOSTOS' | 'CORPO_ABERTO';

/** `MODELO`: peso medido no arquivo 3D atual da versão; `MANUAL`: estimado (versão sem 3D). */
export type OrigemDoPeso = 'MANUAL' | 'MODELO';

/** Uma liga: peso da peça → peso a fundir (com a perda) → custo = peso a fundir × cotação do puro × pureza. */
export interface CustoDoMetalDto {
  readonly ligaId: number;
  readonly liga: string;
  readonly metal: Metal;
  /** Milésimos (750 = 18k). */
  readonly teor: number;
  readonly ligaAtiva: boolean;
  /** Peso da peça pronta (g): o medido no 3D ou o estimado manualmente (ver `origemDoPeso`). */
  readonly pesoEstimado: number;
  readonly pesoAFundir: number;
  /** R$/g do metal puro; nulo sem cotação no projeto. */
  readonly cotacaoDoPuro: number | null;
  /** R$/g da liga (cotação × pureza). */
  readonly valorPorGrama: number | null;
  readonly custo: number;
  readonly pesoReal: number | null;
  /** % do real sobre o estimado (+4 = 4% mais pesada). */
  readonly diferencaDoPesoReal: number | null;
}

export interface CustoDaPecaDto {
  readonly pecaId: number;
  readonly nome: string;
  readonly quantidade: number;
  /** Nulo: peça sem custo na biblioteca. */
  readonly custoUnitario: number | null;
  readonly custo: number;
}

export interface CustoDaMaoDeObraDto {
  /** Nulo enquanto não foi informada. */
  readonly tipo: TipoMaoDeObra | null;
  readonly horas: number | null;
  readonly valorHora: number | null;
  readonly custo: number;
}

export interface ServicoExternoDto {
  readonly nome: string;
  readonly valor: number;
}

export interface PendenciaDoPrecoDto {
  readonly tipo: TipoDePendencia;
  readonly descricao: string;
  readonly pecaId: number | null;
  readonly ligaId: number | null;
  readonly metal: Metal | null;
  /** Corpos do modelo 3D sem material. */
  readonly corpos: readonly string[];
}

export interface AvisoDoPrecoDto {
  readonly tipo: TipoDeAviso;
  readonly descricao: string;
  /** Corpos do modelo 3D sobrepostos ou aberto. */
  readonly corpos: readonly string[];
}

/** Peso digitado para uma liga, guardado na versão. */
export interface PesoManualDto {
  readonly ligaId: number;
  readonly liga: string;
  readonly peso: number;
}

/** O cálculo linha a linha e os dados do orçamento. Só vem para quem tem `precificacao.ler`. */
export interface CalculoDoPrecoDto {
  readonly origemDoPeso: OrigemDoPeso;
  /** Com 3D, o peso manual guardado e não usado (vale o 3D). Vazio sem 3D. */
  readonly pesosManuaisGuardados: readonly PesoManualDto[];
  readonly metais: readonly CustoDoMetalDto[];
  readonly custoDosMetais: number;
  readonly composicao: readonly CustoDaPecaDto[];
  readonly custoDaComposicao: number;
  readonly maoDeObra: CustoDaMaoDeObraDto;
  readonly servicos: readonly ServicoExternoDto[];
  readonly custoDosServicos: number;
  readonly custoTotal: number;
  readonly perdaPercentual: number;
  /** O aplicado: o da versão ou, sem ele, o do projeto. */
  readonly markup: number;
  /** Nulo quando vale o do projeto. */
  readonly markupDaVersao: number | null;
  readonly taxasPercentual: number;
  readonly arredondarPara: number;
  readonly precoSemArredondar: number;
  readonly pendencias: readonly PendenciaDoPrecoDto[];
  readonly avisos: readonly AvisoDoPrecoDto[];
}

/** Preço da versão, calculado a cada leitura (nada é gravado). */
export interface PrecoDaVersaoDto {
  /** Preço final, arredondado. Parcial quando incompleto. */
  readonly preco: number;
  readonly status: StatusDoPreco;
  /** Ausente quando não foi pedido (sem `precificacao.ler`). */
  readonly calculo?: CalculoDoPrecoDto | null;
}

/** Substitui as ligas da versão (peso estimado em gramas). */
export interface PutMetaisDaVersaoDto {
  readonly metais: readonly { readonly ligaId: number; readonly pesoGramas: number }[];
}

export interface PutMaoDeObraDto {
  readonly tipo: TipoMaoDeObra;
  readonly horas?: number | null;
  readonly valor?: number | null;
}

export interface PutServicosDto {
  readonly servicos: readonly ServicoExternoDto[];
}

/** Nulo volta a valer o markup do projeto. */
export interface PutMarkupDaVersaoDto {
  readonly markup: number | null;
}

/** Nulo apaga o peso real. */
export interface PutPesoRealDto {
  readonly pesoGramas: number | null;
}

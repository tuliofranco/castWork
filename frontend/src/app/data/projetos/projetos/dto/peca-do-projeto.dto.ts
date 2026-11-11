import type { TipoPeca } from '@shared/pecas/tipo-peca';

/** `RASCUNHO`: só neste projeto; `PUBLICADA`: está na biblioteca. */
export type StatusPecaDoProjeto = 'RASCUNHO' | 'PUBLICADA';

/** Peça do projeto: criada nele ou importada da biblioteca. É a que entra na composição das versões. */
export interface PecaDoProjetoDto {
  readonly id: number;
  readonly nome: string;
  readonly tipo: TipoPeca;
  /** A liga do ateliê escolhida (todo tipo menos pedra); nula na pedra, sem liga ou em peça anterior à liga. */
  readonly ligaId: number | null;
  /** Nome copiado da liga quando ela foi escolhida, ou o material livre da pedra. */
  readonly material: string | null;
  /** Milésimos (750 = 18k), copiado da liga. */
  readonly teor: number | null;
  readonly pesoGramas: number | null;
  readonly descricao: string | null;
  readonly ativo: boolean;
  readonly status: StatusPecaDoProjeto;
  /** A peça da biblioteca de que esta é cópia; nulo em rascunho. */
  readonly pecaId: number | null;
  /**
   * Custo de uma unidade neste projeto (R$). Só vem quando a consulta pede (`comValor`, para quem tem
   * `precificacao.ler`); `null` = ainda sem valor.
   */
  readonly valor?: number | null;
}

/**
 * O arquivo final de uma ferramenta da peça (`desenho-2d` ou `modelo-3d`): o desenho que vale para o peso, a montagem, a
 * publicação e o visualizador. `nome`: o dado pelo joalheiro (nulo usa o padrão da ferramenta).
 */
export interface ArquivoFinalDaPecaDto {
  readonly ferramenta: string;
  readonly arquivoId: number;
  readonly nome: string | null;
  readonly criadoEm: string;
}

/**
 * De onde vem o peso da peça: `DIGITADO` (sem modelo 3D), `MODELO` (medido no modelo 3D, na liga da peça),
 * `MODELO_SEM_LIGA` (tem 3D, mas sem liga: vale o digitado) ou `MODELO_NAO_MEDIDO` (tem 3D, mas não foi possível medir).
 */
export type OrigemDoPesoDaPeca = 'DIGITADO' | 'MODELO' | 'MODELO_SEM_LIGA' | 'MODELO_NAO_MEDIDO';

/** O peso que vale para a peça (g; nulo sem peso) e o digitado nos dados dela, que fica guardado quando vale o 3D. */
export interface PesoDaPecaDto {
  readonly gramas: number | null;
  readonly origem: OrigemDoPesoDaPeca;
  readonly pesoDigitado: number | null;
}

/** O material é a liga (`ligaId`, todo tipo menos pedra) ou, na pedra, o texto livre (`material`). */
export interface PostPecaDoProjetoDto {
  readonly nome: string;
  readonly tipo: TipoPeca;
  /** Liga ativa do ateliê; a mesma liga já gravada mantém a cópia do nome e do teor, ainda que desativada. */
  readonly ligaId?: number | null;
  /** Só na pedra. */
  readonly material?: string | null;
  readonly pesoGramas?: number | null;
  readonly descricao?: string | null;
}

/** Numa peça publicada, mudar algum dado faz dela outra peça (volta a rascunho). */
export interface PutPecaDoProjetoDto extends PostPecaDoProjetoDto {
  readonly ativo: boolean;
}

/** Nulo tira o valor. */
export interface PutValorDaPecaDto {
  readonly valor: number | null;
}

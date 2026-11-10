import type { StatusVersao } from '@data/projetos/projetos/dto/projeto.dto';
import type { TipoPeca } from '@shared/pecas/tipo-peca';

/** Peça da biblioteca. Não tem valor: o valor é de cada projeto que a usa. */
export interface PecaDto {
  readonly id: number;
  readonly nome: string;
  readonly tipo: TipoPeca;
  readonly material: string | null;
  /** Milésimos (750 = 18k). */
  readonly teor: number | null;
  readonly pesoGramas: number | null;
  readonly ativo: boolean;
}

/** Uma versão do projeto cuja composição usa a peça. */
export interface VersaoQueUsaPecaDto {
  readonly versaoId: number;
  readonly numero: number;
  readonly nome: string;
  readonly status: StatusVersao;
  readonly quantidade: number;
}

/** Um projeto que usa a peça (pela cópia vinculada a ela). */
export interface UsoDaPecaDto {
  readonly projetoId: number;
  readonly projeto: string;
  readonly pecaDoProjetoId: number;
  /** Vazia se o projeto ainda não usou a peça numa versão. */
  readonly versoes: readonly VersaoQueUsaPecaDto[];
  /** Valor no projeto (R$). Só vem com `comValor` (`precificacao.ler`); `null` = sem valor. */
  readonly valor?: number | null;
}

/** Um desenho da peça (`desenho-2d` ou `modelo-3d`), copiado do projeto ao publicar; o conteúdo vem por `getDesenho`. */
export interface DesenhoDaPecaDto {
  readonly ferramenta: string;
  readonly copiadoEm: string;
  /** Bytes. */
  readonly tamanho: number;
}

/**
 * Peça com a descrição, o desenho 2D e o modelo 3D (só para ver) e, quando pedido (`comHistorico`, para quem tem
 * `projetos.ler`), o histórico de uso.
 */
export interface PecaDetalheDto extends PecaDto {
  readonly descricao: string | null;
  readonly desenhos: readonly DesenhoDaPecaDto[];
  readonly historicoDeUso?: readonly UsoDaPecaDto[] | null;
}

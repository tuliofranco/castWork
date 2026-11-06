export type TipoJoia = 'Anel' | 'Pulseira' | 'Colar' | 'Brinco' | 'Pingente' | 'Outro';

export type PapelMensagem = 'Joalheiro' | 'Assistente';

export interface DocumentoBriefingDto {
  readonly id: number;
  readonly nomeArquivo: string;
  readonly geradoEm: string;
}

export interface BriefingDto {
  readonly id: number;
  readonly projetoId: number;
  readonly tipoJoia: TipoJoia;
  readonly oQueOClienteQuer: string;
  readonly ocasiao: string | null;
  readonly materiais: string | null;
  readonly medidas: string | null;
  /** ISO (aaaa-mm-dd). */
  readonly prazo: string | null;
  readonly orcamento: string | null;
  readonly oQueSeraFeito: string | null;
  readonly acabamento: string | null;
  readonly pedras: string | null;
  readonly fecho: string | null;
  readonly observacoes: string | null;
  readonly documentos: readonly DocumentoBriefingDto[];
}

export interface PutFormularioBriefingDto {
  readonly tipoJoia: TipoJoia;
  readonly oQueOClienteQuer: string;
  readonly ocasiao: string | null;
  readonly materiais: string | null;
  readonly medidas: string | null;
  readonly prazo: string | null;
  readonly orcamento: string | null;
}

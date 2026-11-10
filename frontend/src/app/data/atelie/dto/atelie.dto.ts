export interface EnderecoDto {
  readonly cep: string;
  readonly logradouro: string;
  readonly numero: string;
  readonly complemento: string | null;
  readonly bairro: string;
  readonly cidade: string;
  readonly uf: string;
}

export interface ConfiguracaoAtelieDto {
  readonly nome: string;
  readonly documento: string | null;
  readonly email: string | null;
  readonly telefone: string | null;
  readonly endereco: EnderecoDto | null;
  readonly assinatura: string | null;
  readonly corMarca: string;
  readonly instagram: string | null;
  readonly site: string | null;
  readonly avisoBriefing: string | null;
  readonly possuiLogo: boolean;
}

export interface PutAtelieDto {
  readonly nome: string;
  readonly documento: string | null;
  readonly email: string | null;
  readonly telefone: string | null;
  readonly endereco: EnderecoDto | null;
  readonly assinatura: string | null;
  readonly corMarca: string | null;
  readonly instagram: string | null;
  readonly site: string | null;
  readonly avisoBriefing: string | null;
}

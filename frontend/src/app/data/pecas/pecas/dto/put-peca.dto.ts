import type { PostPecaDto } from './post-peca.dto';

/** Mesmos campos do cadastro. Custo nulo tira o custo; sem `precificacao.ler`, o servidor mantém o atual. */
export type PutPecaDto = PostPecaDto;

import type { StatusPecaDoProjeto } from '@data/projetos/projetos/dto/peca-do-projeto.dto';
import type { TomSelo } from '@shared/ui/selo.component';

export const STATUS_PECA_DO_PROJETO: Readonly<
  Record<StatusPecaDoProjeto, { readonly rotulo: string; readonly tom: TomSelo; readonly icone: string; readonly descricao: string }>
> = {
  RASCUNHO: { rotulo: 'Rascunho', tom: 'aviso', icone: 'pencil', descricao: 'Só neste projeto. Publique para colocá-la na biblioteca e liberar as versões que a usam.' },
  PUBLICADA: { rotulo: 'Publicada', tom: 'sucesso', icone: 'check2-circle', descricao: 'Está na biblioteca.' },
};

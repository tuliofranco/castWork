import type { StatusVersao } from '@data/projetos/projetos/dto/projeto.dto';
import type { TomSelo } from '@shared/ui/selo.component';

export const STATUS_VERSAO: Readonly<Record<StatusVersao, { readonly rotulo: string; readonly tom: TomSelo; readonly icone: string }>> = {
  EM_DESENVOLVIMENTO: { rotulo: 'Em desenvolvimento', tom: 'aviso', icone: 'pencil' },
  PUBLICADA: { rotulo: 'Publicada', tom: 'sucesso', icone: 'check2-circle' },
  ARQUIVADA: { rotulo: 'Arquivada', tom: 'neutro', icone: 'archive' },
};

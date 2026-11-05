import type { StatusVersao } from '@data/projetos/projetos/dto/projeto.dto';

export const STATUS_VERSAO: Readonly<Record<StatusVersao, { readonly rotulo: string; readonly classe: string }>> = {
  EM_DESENVOLVIMENTO: { rotulo: 'Em desenvolvimento', classe: 'text-bg-warning' },
  PUBLICADA: { rotulo: 'Publicada', classe: 'text-bg-success' },
};

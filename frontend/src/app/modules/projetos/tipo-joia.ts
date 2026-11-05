import type { TipoJoia } from '@data/projetos/briefing/dto/briefing.dto';

export const TIPOS_JOIA: readonly { readonly valor: TipoJoia; readonly rotulo: string }[] = [
  { valor: 'Anel', rotulo: 'Anel' },
  { valor: 'Pulseira', rotulo: 'Pulseira' },
  { valor: 'Colar', rotulo: 'Colar' },
  { valor: 'Brinco', rotulo: 'Brinco' },
  { valor: 'Pingente', rotulo: 'Pingente' },
  { valor: 'Outro', rotulo: 'Outro' },
];

/** Atalhos de material no formulário; o campo continua livre para o que não estiver aqui. */
export const MATERIAIS_COMUNS: readonly string[] = ['Ouro amarelo', 'Ouro branco', 'Ouro rosé', 'Prata', 'Pedras'];

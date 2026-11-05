import type { TokenPayload } from '@core/models/token-payload.model';

type UsuarioDemo = Omit<TokenPayload, 'exp'>;

/** Senha aceita pelo login em modo mock. */
export const SENHA_DEMO = 'castwork';

/**
 * Dois perfis para testar permissões manualmente:
 * - joalheira: acesso completo;
 * - aprendiz: só vê Peças e Portfólio (o resto some do menu e cai no 403 pela URL).
 */
export const USUARIOS_DEMO: Readonly<Record<string, UsuarioDemo>> = {
  'joalheira@castwork.dev': {
    sub: 'usr-1',
    nome: 'Joana Ourives',
    email: 'joalheira@castwork.dev',
    atelieId: 'atl-1',
    atelieNome: 'Ateliê Pedra Bruta',
    permissoes: [
      'clientes.ler',
      'clientes.editar',
      'pecas.ler',
      'pecas.editar',
      'precificacao.ler',
      'precificacao.editar',
      'documentos.ler',
      'portfolio.ler',
    ],
  },
  'aprendiz@castwork.dev': {
    sub: 'usr-2',
    nome: 'Caio Aprendiz',
    email: 'aprendiz@castwork.dev',
    atelieId: 'atl-1',
    atelieNome: 'Ateliê Pedra Bruta',
    permissoes: ['pecas.ler', 'portfolio.ler'],
  },
};

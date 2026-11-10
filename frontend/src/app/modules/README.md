# modules/

Telas por domínio, **sempre com lazy loading** (`loadChildren` / `loadComponent`).

`clientes/`, `pecas/`, `precificacao/`, `documentos/`, `portfolio/`

- `pecas/`: biblioteca (lista, `nova` e `:id/editar`). Custo por unidade e selo "Sem custo" só com `precificacao.ler`; edição com `pecas.editar`.
- `precificacao/`: preços do ateliê (cotações com histórico, ligas e parâmetros). Sem `precificacao.editar`, só leitura.
- `projetos/`: na página do projeto, o bloco **Preços deste projeto** (`cw-precos-projeto`): herdado em, editar, atualizar para os valores atuais (com confirmação) e a diferença para o ateliê de hoje. Só aparece com `precificacao.ler`; editar e atualizar pedem `precificacao.editar`.

**Regra central:** um módulo nunca importa outro (o ESLint bloqueia imports de `@modules/*`). O que precisar ser compartilhado sobe para `shared/` ou `data/`.

# modules/

Telas por domínio, **sempre com lazy loading** (`loadChildren` / `loadComponent`).

`clientes/`, `pecas/`, `precificacao/`, `documentos/`, `portfolio/`

- `pecas/`: biblioteca (lista, `nova` e `:id/editar`). Custo por unidade e selo "Sem custo" só com `precificacao.ler`; edição com `pecas.editar`.
- `precificacao/`: preços do ateliê (cotações com histórico, ligas e parâmetros). Sem `precificacao.editar`, só leitura.

**Regra central:** um módulo nunca importa outro (o ESLint bloqueia imports de `@modules/*`). O que precisar ser compartilhado sobe para `shared/` ou `data/`.

# modules/

Telas por domínio, **sempre com lazy loading** (`loadChildren` / `loadComponent`).

`clientes/`, `pecas/`, `precificacao/`, `documentos/`, `portfolio/`

**Regra central:** um módulo nunca importa outro (o ESLint bloqueia imports de `@modules/*`). O que precisar ser compartilhado sobe para `shared/` ou `data/`.

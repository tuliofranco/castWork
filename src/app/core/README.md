# core/

Infraestrutura **essencial e única** da aplicação. Tudo aqui existe uma vez só e é configurado no `app.config.ts`.

| Pasta | Conteúdo |
|---|---|
| `guards/` | `CanActivateFn` funcionais (`authGuard`, `permissionGuard`) |
| `interceptors/` | `HttpInterceptorFn`, um por responsabilidade (URL base, JWT, nulos, datas, erro, loading) |
| `error-handlers/` | `GlobalErrorHandler` |
| `factories/` | factory do Apollo (`InMemoryCache`, `typePolicies`, `fetchPolicy`) |
| `services/` | auth, jwt, sessão, permissão |
| `validators/` | validators de formulário com regra de negócio global |
| `models/` | tipos globais (ex.: payload do token, permissões) |

**Regra:** `core/` não importa `modules/`. Se algo for reutilizável sem contexto global, vai para `shared/`.

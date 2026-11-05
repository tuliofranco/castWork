# shared/

Peças **reutilizáveis e independentes de contexto**. Nada aqui conhece um domínio específico.

- `components/` modal, offcanvas, paginação, filtro base, upload de imagem, CVAs (endereço, precificação)
- `directives/`, `pipes/`
- `operators/` operadores RxJS próprios
- `injection-tokens/` `InjectionToken`s e `HttpContextToken`s
- `services/` toast, confirmação, loading overlay, paginação, local storage

**Regra:** `shared/` não importa `core/` com estado de sessão nem `modules/`.

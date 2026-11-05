# Segurança do backend Castwork

Requisitos de segurança obrigatórios da API. Cada item diz **o que** será feito, **por quê**, **em qual etapa** e
**como verificar**. Nenhuma etapa é considerada concluída enquanto os itens de segurança atribuídos a ela não
estiverem implementados e testados.

> Contexto: instalação **single-tenant** (um ateliê por instalação, um banco). Não há tenant no token; o
> equivalente ao risco de "vazamento entre tenants" aqui é **vazamento entre usuários/perfis** e **IDOR**
> (acessar um recurso só trocando o Id na URL). As regras do item 3 foram adaptadas para isso.

---

## 1. Transporte

| Requisito | Implementação | Etapa |
|---|---|---|
| HTTPS obrigatório | `UseHttpsRedirection()` sempre; `UseHsts()` fora de Development (`max-age` 1 ano, `includeSubDomains`). | 7 |
| TLS ≥ 1.2 | Kestrel com `SslProtocols = Tls12 \| Tls13`. Em Docker, TLS também entre proxy e API (certificado interno), nada de HTTP na rede interna. | 7, 12 |
| Proxy/gateway | `UseForwardedHeaders` com `XForwardedFor \| XForwardedProto`, **`KnownProxies`/`KnownNetworks` explícitos** (sem isso qualquer cliente forja `X-Forwarded-For` e burla o rate limit por IP). Deve ser o primeiro middleware. | 7, 12 |

**Por quê:** JWT e refresh token são credenciais ao portador; quem intercepta, usa. HSTS impede o downgrade para
HTTP depois da primeira visita.

**Verificar:** `curl -I http://...` → 307/308 para HTTPS; `curl --tlsv1.1 --tls-max 1.1 https://...` falha;
header `Strict-Transport-Security` presente em produção.

---

## 2. Autenticação

| Requisito | Implementação | Etapa |
|---|---|---|
| Validação completa do JWT | `ValidateIssuer`, `ValidateAudience`, `ValidateIssuerSigningKey`, `ValidateLifetime` = `true`; `RequireExpirationTime`, `RequireSignedTokens`; `ValidAlgorithms = [HS256]` (bloqueia `alg: none` e troca de algoritmo); `ClockSkew = 30s`. `MapInboundClaims = false` para as claims chegarem com o nome emitido. | 6 |
| Vida curta + refresh | Access token **15 min**. Refresh token opaco (256 bits aleatórios), guardado **só como hash SHA-256** no banco, vida de 7 dias, **rotação a cada uso** e **detecção de reuso**: usar um refresh já trocado revoga toda a família (sinal de roubo). Revogação no logout, na troca de senha e na desativação do usuário. | 6 |
| Chave fora do código | `Jwt:ChaveAssinatura` vem de User Secrets (local) ou variável de ambiente/cofre (produção). Na inicialização: falha se a chave estiver ausente ou tiver < 256 bits (`ValidateOnStart`). | 6 |
| Serviço para serviço | Credencial própria por serviço (client credentials com escopo restrito ou API key com hash no banco), esquema de autenticação **separado** do de usuário e policy própria. Token de usuário nunca é repassado. *Ainda não há consumidor desse tipo; o esquema fica preparado e desligado.* | 6 |

**Adicionado (fora da lista, mas necessário):**
- Senha com hash **PBKDF2 via `PasswordHasher<T>`** do ASP.NET Core Identity (salt + iterações altas, rehash automático quando o algoritmo evolui). Nunca guardar senha reversível.
- **Lockout**: 5 tentativas erradas → bloqueio de 15 min. Mensagem de erro de login genérica ("usuário ou senha inválidos"), sem dizer qual dos dois errou.
- `ISessaoService` lê **apenas** as claims do token validado; nada de identidade vinda de header, query ou body.
- Claim de `versao_seguranca` (security stamp) no token: trocar senha ou perfil invalida os access tokens já emitidos no próximo refresh.

**Verificar (testes de integração):** token expirado → 401; assinatura alterada → 401; `alg: none` → 401;
issuer/audience errados → 401; refresh reutilizado → 401 e família revogada; 6ª senha errada → bloqueio.

---

## 3. Autorização

| Requisito | Implementação | Etapa |
|---|---|---|
| Negar por padrão | `FallbackPolicy = RequireAuthenticatedUser()`. Endpoints públicos (login, refresh, health) com `[AllowAnonymous]` explícito. No MediatR, request sem `[Permissao]` nem `[SemPermissao]` **falha num teste de arquitetura**, então ninguém esquece de declarar. | 5, 7 |
| Permissões declarativas | `[Permissao("guid")]` na request + `AuthorizationBehaviour` (o mesmo caminho para REST e GraphQL). Nenhum `if (usuario.IsAdmin)` espalhado em handler. | 5 |
| Isolamento (adaptado ao single-tenant) | Sem `AtelieId`. Identidade **só do token**. Quando houver dado restrito por usuário, o filtro sai do `ISessaoService`, nunca de parâmetro da requisição. Ids sequenciais `long` são aceitáveis porque toda leitura passa pela autorização; **testes de IDOR** para cada recurso. | 5, 11 |
| Painel do Hangfire | Filtro de autorização próprio exigindo admin; nunca o `LocalRequestsOnlyAuthorizationFilter` padrão atrás de proxy. | 10 |

**Verificar:** teste de arquitetura sobre todas as requests; teste de integração em que um usuário sem a regra
recebe 403 tanto no REST quanto no GraphQL para o mesmo caso de uso.

---

## 4. Segredos e configuração

| Requisito | Implementação | Etapa |
|---|---|---|
| Nada de segredo versionado | `appsettings.json` só com valores não sensíveis e placeholders vazios. Local: `dotnet user-secrets`. Produção: variáveis de ambiente / cofre. Options com `ValidateDataAnnotations().ValidateOnStart()`: segredo faltando derruba a API na subida, e não na primeira requisição. | 3, 6 |
| Detecção de vazamento | `gitleaks` no CI e como hook de pre-commit opcional. | 12 |
| Banco com menor privilégio | **Dois roles PostgreSQL**: `castwork_migrator` (dono do schema, com DDL; usado só para aplicar migrations) e `castwork_app` (apenas `SELECT/INSERT/UPDATE/DELETE` nas tabelas e `USAGE` nas sequences, via `ALTER DEFAULT PRIVILEGES`; sem DDL, sem `SUPERUSER`). Configuração: `ConnectionStrings:CastworkMigrations` (opcional) e `ConnectionStrings:Castwork`. Sem a primeira, a API não aplica migrations e o deploy usa `efbundle`. `REVOKE CREATE ON SCHEMA public FROM PUBLIC`. | 3 ✅ (código), 12 (roles no compose) |
| Hangfire | `Hangfire.PostgreSql` em schema próprio (`hangfire`), com permissões só nesse schema. | 10 |

---

## 5. Entrada

| Requisito | Implementação | Etapa |
|---|---|---|
| Validação centralizada | `ValidationBehaviour` (FluentValidation) no pipeline do MediatR; REST e GraphQL passam pelo mesmo pipeline. | 4, 5 |
| Limite de corpo | Kestrel `MaxRequestBodySize = 1 MB` global; `[RequestSizeLimit]` maior só no upload de foto (10 MB) e `FormOptions.MultipartBodyLengthLimit` igual. | 7 |
| Upload seguro | Validar tipo pelo **conteúdo (magic bytes)**, não pela extensão nem pelo `Content-Type`; aceitar só JPEG/PNG/WebP/HEIC; **regravar a imagem** (re-encode) ao gerar as miniaturas, o que remove EXIF (inclusive GPS) e payloads embutidos; nome no blob gerado pelo servidor (GUID), nunca o nome original; blob privado, entregue por URL assinada de curta duração ou pela própria API. | 10 |
| GraphQL | Profundidade máxima (ex.: 8), análise de custo/complexidade do HotChocolate com orçamento por requisição, `MaxPageSize` = 100 no paging, introspecção e Nitro desligados em produção, e **sem batching** de operações. | 8 |
| Paginação | `TamanhoPagina` máximo de 50 (fora da faixa vira o padrão) e `OrdenarPor` só aceita campos do DTO (senão 400). | 4 ✅ |

---

## 6. Superfície exposta

| Requisito | Implementação | Etapa |
|---|---|---|
| CORS | Policy nomeada com `WithOrigins(config)` explícito (a origem do front Angular), métodos e headers listados. `AllowAnyOrigin` proibido; um teste falha se a lista estiver vazia ou tiver `*` em produção. | 7 |
| Rate limiting | `AddRateLimiter`: **login e refresh** com janela fixa por IP **e** por nome de usuário (ex.: 5/min); API autenticada com token bucket por usuário; resposta 429 com `Retry-After`. Depende do `ForwardedHeaders` correto (item 1). | 7 |
| Headers de segurança | Middleware próprio: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` nas respostas da API; CSP específica e menos restrita só nas páginas HTML (Scalar em dev, painel Hangfire). Remover o header `Server` do Kestrel. | 7 |
| Documentação da API | Scalar/OpenAPI só em Development (e opcionalmente em homologação, atrás de autenticação). | 7 |

---

## 7. Erros e logs

| Requisito | Implementação | Etapa |
|---|---|---|
| ProblemDetails | `IExceptionHandler` global mapeando para 400/401/403/404/422; 500 com mensagem genérica e só o `traceId`. `UseDeveloperExceptionPage` só em Development. O `ErrorFilter` do GraphQL segue a mesma regra (sem `IncludeExceptionDetails` fora de dev). | 7, 8 |
| Correlation id | Usar o **trace id do W3C `traceparent`** (OpenTelemetry) como correlation id: entra no log, no ProblemDetails e no header de resposta, e se propaga sozinho para Refit/HttpClient e jobs do Hangfire. | 7, 12 |
| Sem dado sensível no log | Log estruturado com template (nunca interpolação); **nunca** logar corpo de requisição, header `Authorization`, senha, token, refresh token, CPF/CNPJ, e-mail e telefone; ao logar documento, usar uma versão mascarada (`***.982.247-**`). Teste que faz login e verifica que a senha não aparece em nenhum log capturado. | 5, 7, 12 |

---

## 8. Dependências

| Requisito | Implementação | Etapa |
|---|---|---|
| Central Package Management | Já implementado (`Directory.Packages.props` + pinning transitivo). | 1 ✅ |
| Auditoria de vulnerabilidades | **NuGet Audit no build**: `NuGetAudit=true`, `NuGetAuditMode=all` (inclui transitivas), `NuGetAuditLevel=low`. Com `TreatWarningsAsErrors`, um pacote vulnerável **quebra o build localmente**, e não só no CI. No CI, ainda: `dotnet list package --vulnerable --include-transitive`. | 1 ✅ (build), 12 (CI) |
| Atualização | Dependabot/Renovate agrupando por família de pacote. | 12 |

---

## Riscos específicos do Castwork (além da lista)

- **LGPD:** o cliente do ateliê tem CPF, endereço e telefone. É preciso finalidade definida, exportação e anonimização sob pedido (Etapa 4+, módulo Clientes) e dados pessoais fora dos logs (item 7).
- **PDF:** os documentos são gerados com dados digitados pelo usuário. O gerador não pode interpretar HTML ou script dos campos, e o download passa por autorização, nunca por URL pública previsível.
- **Cotação externa (Refit):** timeout, circuit breaker e validação de faixa do valor retornado. Uma API comprometida não pode sugerir um preço absurdo sem alerta.

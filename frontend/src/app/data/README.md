# data/

Camada de dados **sem estado**. Um service aqui só monta a requisição e devolve `Observable<Dto>`.

```
data/<dominio>/<recurso>/
  <recurso>.service.ts     # chamadas REST/GraphQL
  dto/                     # VerboRecursoDto (verbo-recurso.dto.ts), GetRecursoQuery (.query.ts)
  queries/                 # documentos gql + GetRecursoQueryGql (.query-gql.ts)
  mocks/                   # fábricas com @faker-js/faker
```

Domínios: `clientes`, `pecas` (o `custo` só é pedido com `comCusto`, para quem tem `precificacao.ler`), `precificacao` (`precos`: cotações, ligas e parâmetros do ateliê), `projetos` (os preços do projeto vêm em `getPrecosDoProjeto`, separados do `getProjeto`, porque exigem `precificacao.ler`; o preço da versão vem em `getPrecoDaVersao`, com o `calculo` só quando `comCalculo`, e o orçamento é gravado por `putMetaisDaVersao`, `putMaoDeObra`, `putServicos`, `putMarkupDaVersao` e `putPesoReal`), `documentos`, `portfolio`. O que é compartilhado entre domínios vai em `common/`.

**Proibido:** guardar estado (signals/BehaviorSubject), nomes genéricos (`Dados`) ou voltados à UI (`FormularioPeca`).

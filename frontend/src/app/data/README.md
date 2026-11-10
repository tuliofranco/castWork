# data/

Camada de dados **sem estado**. Um service aqui só monta a requisição e devolve `Observable<Dto>`.

```
data/<dominio>/<recurso>/
  <recurso>.service.ts     # chamadas REST/GraphQL
  dto/                     # VerboRecursoDto (verbo-recurso.dto.ts), GetRecursoQuery (.query.ts)
  queries/                 # documentos gql + GetRecursoQueryGql (.query-gql.ts)
  mocks/                   # fábricas com @faker-js/faker
```

Domínios: `clientes`, `pecas` (biblioteca só de leitura: `getPecas` e `getPeca`, este com os `desenhos` (sem conteúdo; o conteúdo em `getDesenho`, REST) e com o `historicoDeUso` só quando `comHistorico` (`projetos.ler`) e o valor de cada uso só quando `comValor` (`precificacao.ler`); sem valor na peça), `precificacao` (`precos`: cotações, ligas e parâmetros do ateliê; `getLigas` traz só as ligas ativas, com `projetos.ler`, para o seletor de liga das peças), `projetos` (as peças do projeto vêm em `getProjeto` (sem valor, para a composição e o status ao publicar) e em `getPecasDoProjeto`, com o `valor` só quando `comValor`; a peça traz `ligaId`, `material` (nome copiado da liga ou material livre da pedra) e `teor`; o peso que vale para a peça (`PesoDaPecaDto`: `gramas`, `origem` e `pesoDigitado`) vem em `getPesoDaPeca` (só a peça pedida, `pecas(id:)`) e o de cada peça da composição em `getPesosDaVersao`, separados porque medem o modelo 3D; escritas em `postPecaDoProjeto` e `putPecaDoProjeto` com `ligaId` ou, na pedra, `material`, e em `postImportarPeca`, `putValorDaPeca` e `postPublicarPeca`; o item da composição usa `pecaDoProjetoId`; os preços do projeto vêm em `getPrecosDoProjeto`, separados do `getProjeto`, porque exigem `precificacao.ler`; o preço da versão vem em `getPrecoDaVersao`, com o `calculo` só quando `comCalculo` (com a `origemDoPeso`, `MODELO` ou `MANUAL`, e os `pesosManuaisGuardados` quando vale o 3D), e o orçamento é gravado por `putMetaisDaVersao`, `putMaoDeObra`, `putServicos`, `putMarkupDaVersao` e `putPesoReal`), `ferramentas` (`briefing`; `modelo-3d`: a conferência de peso e materiais em `getConferencia`, pelo GraphQL `conferenciaDoModelo` da versão, e as correções em `putMateriais` e `postUnirCorpos`, que devolvem o arquivo novo e o de antes; `postMontarAPartirDasPecas` monta o modelo da versão a partir das peças), `assistente` (`FerramentasService`: a conversa e o arquivo de cada ferramenta pelo escopo `EscopoDaFerramenta`: a versão (Id), a peça do projeto (`{ pecaId }`) ou o projeto (`null`)), `documentos`, `portfolio`. O que é compartilhado entre domínios vai em `common/`.

**Proibido:** guardar estado (signals/BehaviorSubject), nomes genéricos (`Dados`) ou voltados à UI (`FormularioPeca`).

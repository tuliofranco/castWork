# Precificação

Preços do ateliê: a base de todo orçamento. Modelo aprovado em [`docs/PRECIFICACAO.md`](../../../../docs/PRECIFICACAO.md).

- **Cotações** (`CotacaoMetal`): R$/g do metal **puro** (`Metal`: Ouro, Prata), só INSERT. A vigente é a mais recente;
  as anteriores formam o histórico, com data e autor.
- **Ligas** (`Liga`): nome, metal, `TeorMetal` e densidade (g/cm³). Sete pré-cadastradas pela migration (Ids 1–7;
  as do ateliê começam em 1000). Não se exclui: desativa.
- **Parâmetros** (`ParametrosDePreco`, value object na `ConfiguracaoAtelie`): perda, valor da hora, markup, taxas e
  arredondamento.
- **Preços do projeto** (etapa 3): o `Projeto` copia os preços do ateliê ao nascer (`PrecosDoAtelie`, montado por
  `PrecosDoAtelieExtensions.ObterPrecosDoAtelieAsync`): cotação vigente de cada metal já cotado (`CotacaoDoProjeto`, com
  a data de origem) e os parâmetros, mais `PrecosHerdadosEm`. Editar (`AlterarPrecos`) marca `PrecosAlteradosEm`;
  "atualizar para os valores atuais" (`AtualizarPrecosDoAtelie`) substitui tudo e limpa a marca. Um nunca altera o outro.

| Camada | Onde |
|---|---|
| Domain | `Domain/Entities/Precificacao` (`Metal`, `CotacaoMetal`, `Liga`), `Domain/ValueObjects/ParametrosDePreco.cs` |
| Application | `Application/Precificacao`: commands `AtualizarCotacao`, `CriarLiga`, `AlterarLiga`, `DesativarLiga`, `AtualizarParametrosDePreco`; query `GetPrecosDoAtelie`. Faixas dos parâmetros em `ParametrosDePrecoExtensions` (ateliê e projeto) |
| Application (projeto) | `Application/Projetos`: `CriarProjeto` herda; commands `AlterarPrecosDoProjeto` e `AtualizarPrecosDoProjeto`; projeção `ParaPrecosDto` |
| Infra | `Infra/Data/Configurations/Precificacao` (tabelas `cotacoes_metal` e `ligas`, seed das ligas); colunas dos parâmetros em `configuracao_atelie`; no projeto, colunas em `projetos` e tabela `projeto_cotacoes` |
| REST | `PrecificacaoController` (`api/precificacao`): `POST cotacoes`, `POST ligas`, `PUT ligas/{id}`, `POST ligas/{id}/desativar`, `PUT parametros`. `ProjetosController`: `PUT api/projetos/{id}/precos`, `POST api/projetos/{id}/precos/atualizar` |
| GraphQL | `PrecificacaoQueries`: `precosDoAtelie` e `historicoDeCotacoes(metal)` (paginado), com a policy `precificacao.ler`. `Projeto.precos` (`PrecosDoProjetoType`, por DataLoader): datas livres, `cotacoes` e `parametros` com `precificacao.ler` |
| Front | `modules/precificacao` (tela), `data/precificacao/precos` (serviço); bloco `cw-precos-projeto` na página do projeto |

**Permissões:** ler com `Regras.Precificacao.Consultar` (`precificacao.ler`); alterar com `Regras.Precificacao.Editar`
(`precificacao.editar`). O valor da hora saiu dos dados do ateliê (`AtualizarConfiguracaoAtelie`): agora é parâmetro de
preço, com a regra de precificação.

Editar e atualizar os preços de um projeto exigem `precificacao.editar`; criar o projeto não (a herança é automática).

**Migration `PrecosDoProjeto`:** os projetos que já existiam recebem os parâmetros e as cotações vigentes do ateliê,
com `precos_herdados_em` = data da migration.

**Testes:** o reset do ambiente de integração recria o seed das ligas a partir do `HasData` da configuration.

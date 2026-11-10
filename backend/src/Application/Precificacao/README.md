# Precificação

Preços do ateliê: a base de todo orçamento. Modelo aprovado em [`docs/PRECIFICACAO.md`](../../../../docs/PRECIFICACAO.md).

- **Cotações** (`CotacaoMetal`): R$/g do metal **puro** (`Metal`: Ouro, Prata), só INSERT. A vigente é a mais recente;
  as anteriores formam o histórico, com data e autor.
- **Ligas** (`Liga`): nome, metal, `TeorMetal` e densidade (g/cm³). Sete pré-cadastradas pela migration (Ids 1–7;
  as do ateliê começam em 1000). Não se exclui: desativa.
- **Parâmetros** (`ParametrosDePreco`, value object na `ConfiguracaoAtelie`): perda, valor da hora, markup, taxas e
  arredondamento. O projeto vai copiá-los na etapa 3.

| Camada | Onde |
|---|---|
| Domain | `Domain/Entities/Precificacao` (`Metal`, `CotacaoMetal`, `Liga`), `Domain/ValueObjects/ParametrosDePreco.cs` |
| Application | `Application/Precificacao`: commands `AtualizarCotacao`, `CriarLiga`, `AlterarLiga`, `DesativarLiga`, `AtualizarParametrosDePreco`; query `GetPrecosDoAtelie` |
| Infra | `Infra/Data/Configurations/Precificacao` (tabelas `cotacoes_metal` e `ligas`, seed das ligas); colunas dos parâmetros em `configuracao_atelie` |
| REST | `PrecificacaoController` (`api/precificacao`): `POST cotacoes`, `POST ligas`, `PUT ligas/{id}`, `POST ligas/{id}/desativar`, `PUT parametros` |
| GraphQL | `PrecificacaoQueries`: `precosDoAtelie` e `historicoDeCotacoes(metal)` (paginado), com a policy `precificacao.ler` |
| Front | `modules/precificacao` (tela) e `data/precificacao/precos` (serviço) |

**Permissões:** ler com `Regras.Precificacao.Consultar` (`precificacao.ler`); alterar com `Regras.Precificacao.Editar`
(`precificacao.editar`). O valor da hora saiu dos dados do ateliê (`AtualizarConfiguracaoAtelie`): agora é parâmetro de
preço, com a regra de precificação.

**Testes:** o reset do ambiente de integração recria o seed das ligas a partir do `HasData` da configuration.

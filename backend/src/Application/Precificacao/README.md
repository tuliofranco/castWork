# Precificação

Preços do ateliê, do projeto e da versão. Modelo aprovado em [`docs/PRECIFICACAO.md`](../../../../docs/PRECIFICACAO.md).

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
- **Preço da versão** (etapa 4): a `VersaoProjeto` guarda os dados do orçamento: `Metais` (`MetalDaVersao`: liga, peso
  **estimado** e peso real opcional), `MaoDeObra` (por horas ou valor fixo), `Servicos` (`ServicoExterno`: nome + valor)
  e `Markup` (nulo: vale o do projeto). O preço **não é gravado**: a `CalculadoraDePreco` (DomainService, pura e
  determinística, sem IA) refaz o cálculo a cada leitura. Congelar o preço é papel da proposta.

- **Peso pelo modelo 3D** (etapa 6): quando a versão tem arquivo 3D (o atual do Modelo 3D, não arquivado nem em branco),
  os metais vêm da **medição** desse arquivo, sem IA (`PesoDoModelo` + `IMedidorDoModelo`, em
  `Application/Ferramentas/Modelo3d`): soma da massa (`mass_g`) dos corpos por liga, pelo material de cada corpo. O peso
  manual vale só para versão **sem 3D**; com 3D, fica guardado, não é usado e volta em `PesosManuaisGuardados` para a
  tela dizer isso. O peso real continua por liga da versão (aparece também na linha medida da mesma liga).

### Cálculo do preço da versão

1. **Por liga:** peso (medido no 3D ou, sem 3D, estimado) → peso a fundir = peso × (1 + perda), em mg; custo = peso a fundir × cotação do metal puro × pureza da liga.
2. **Composição:** Σ quantidade × custo unitário da peça. 3. **Mão de obra:** horas × valor da hora do projeto, ou valor
   fixo. 4. **Serviços:** soma. 5. **Custo total.**
6. **Preço** = custo × markup (da versão ou do projeto) × (1 + taxas), arredondado para cima ao múltiplo do projeto.
7. **Status:** `Completo`, ou `Incompleto` com as **pendências** (`TipoDePendencia`): peça sem custo, metal sem cotação no
   projeto, versão sem nenhum metal com peso, liga desativada em uso e, além das da issue, mão de obra por horas com o
   valor da hora do projeto "a definir" (zero). Com 3D, também **corpos sem material** (`CorposSemMaterial`, com os nomes
   em `Corpos`: o material é obrigatório) e **modelo não medido** (`ModeloNaoMedido`: SolidCraft indisponível ou arquivo
   com problema; aí não há metais e não sai a de "sem metal"). O preço parcial sai assim mesmo.
8. **Avisos** que não bloqueiam: cotação de um metal usado com mais de 15 dias; com 3D, **corpos de metal sobrepostos**
   (`CorposSobrepostos`: a parte comum conta duas vezes) e **corpo aberto** de metal ou sem material (`CorpoAberto`: sem
   volume confiável, fica fora do peso).

**Corpos do modelo** (`UsoDoCorpo`, pelo material no arquivo): `Castwork liga {id}` é metal daquela liga; "Pedra" e
"Apoio (não conta)" ficam fora do peso; o "Default" do SolidCraft, nenhum material ou qualquer material que não é do
ateliê é **sem material**. O somatório (`ModeloMedido.PesoPorLiga`) soma a massa medida e só então arredonda ao mg.

O peso real não muda o preço: é a conferência do estimado (`DiferencaDoPesoReal`, em %). Duplicar uma versão copia o
orçamento, sem o peso real. Versão publicada ou arquivada recusa alterar o orçamento (422), mas aceita o peso real.

| Camada | Onde |
|---|---|
| Domain | `Domain/Entities/Precificacao` (`Metal`, `CotacaoMetal`, `Liga`), `Domain/ValueObjects/ParametrosDePreco.cs` |
| Application | `Application/Precificacao`: commands `AtualizarCotacao`, `CriarLiga`, `AlterarLiga`, `DesativarLiga`, `AtualizarParametrosDePreco`; query `GetPrecosDoAtelie`. Faixas dos parâmetros em `ParametrosDePrecoExtensions` (ateliê e projeto) |
| Application (projeto) | `Application/Projetos`: `CriarProjeto` herda; commands `AlterarPrecosDoProjeto` e `AtualizarPrecosDoProjeto`; projeção `ParaPrecosDto` |
| DomainService | `DomainService/Precificacao`: `CalculadoraDePreco` (entrada `EntradaDoPreco`, saída `PrecoCalculado`); `ModeloMedido` (corpos classificados, sobreposições, `PesoPorLiga`) e `OrigemDoPeso` |
| Application (versão) | `Application/Projetos`: commands `DefinirMetaisDaVersao`, `DefinirMaoDeObra`, `DefinirServicos`, `DefinirMarkupDaVersao`, `RegistrarPesoReal` (`Regras.Precificacao.Editar`); query `GetPrecoDaVersao` (`Regras.Projetos.Consultar`; o `Calculo` só sai com `precificacao.ler`; com 3D, mede pelo `IMedidorDoModelo`) |
| Infra | `Infra/Data/Configurations/Precificacao` (tabelas `cotacoes_metal` e `ligas`, seed das ligas); colunas dos parâmetros em `configuracao_atelie`; no projeto, colunas em `projetos` e tabela `projeto_cotacoes`; na versão (`projeto_versoes`), `metais` e `servicos` em jsonb, `mao_de_obra_tipo`, `mao_de_obra_horas`, `mao_de_obra_valor_fixo` e `markup` |
| REST | `PrecificacaoController` (`api/precificacao`): `POST cotacoes`, `POST ligas`, `PUT ligas/{id}`, `POST ligas/{id}/desativar`, `PUT parametros`. `ProjetosController`: `PUT api/projetos/{id}/precos`, `POST api/projetos/{id}/precos/atualizar`; na versão (`api/projetos/{id}/versoes/{versaoId}/`): `PUT metais`, `PUT metais/{ligaId}/peso-real`, `PUT mao-de-obra` (`tipo`: `PorHoras` ou `ValorFixo`), `PUT servicos`, `PUT markup` |
| GraphQL | `PrecificacaoQueries`: `precosDoAtelie` e `historicoDeCotacoes(metal)` (paginado), com a policy `precificacao.ler`. `Projeto.precos` (`PrecosDoProjetoType`, por DataLoader): datas livres, `cotacoes` e `parametros` com `precificacao.ler`. **Preço da versão:** campo calculado `VersaoProjeto.preco` (`PrecoDaVersaoType`, por `PrecoPorVersaoDataLoader`, que envia a `GetPrecoDaVersaoQuery`): `preco` e `status` livres, `calculo` com `precificacao.ler`. Escolhido o GraphQL porque a página da versão já lê por ele; ela pede só a versão aberta com `projeto(id) { versoes(id: $versaoId) { preco } }` (o argumento `id` de `versoes` filtra a lista) |
| Front | `modules/precificacao` (tela), `data/precificacao/precos` (serviço); bloco `cw-precos-projeto` na página do projeto; card `cw-preco-versao` na página da versão |

**Permissões:** ler com `Regras.Precificacao.Consultar` (`precificacao.ler`); alterar com `Regras.Precificacao.Editar`
(`precificacao.editar`). O valor da hora saiu dos dados do ateliê (`AtualizarConfiguracaoAtelie`): agora é parâmetro de
preço, com a regra de precificação.

Editar e atualizar os preços de um projeto exigem `precificacao.editar`; criar o projeto não (a herança é automática).
Montar o orçamento da versão (metais, mão de obra, serviços, markup e peso real) também exige `precificacao.editar`; ler
o preço exige só `projetos.ler`, mas sem `precificacao.ler` vem **só o preço final e o status** (sem custos, markup,
cotações nem pendências).

**Migration `OrcamentoDaVersao`:** colunas do orçamento em `projeto_versoes` (`metais` e `servicos` jsonb com `'[]'` de
default; mão de obra e markup nulos). As versões que já existiam ficam sem orçamento (preço incompleto até informar os
metais).

**Migration `PrecosDoProjeto`:** os projetos que já existiam recebem os parâmetros e as cotações vigentes do ateliê,
com `precos_herdados_em` = data da migration.

**Testes:** o reset do ambiente de integração recria o seed das ligas a partir do `HasData` da configuration. O peso
pelo 3D: unitários em `UnitTests/DomainService/Precificacao/PrecoPeloModeloTests.cs` (somatório por liga, pendências e
avisos) e `UnitTests/Application/Ferramentas/Modelo3d/PesoDoModeloTests.cs` (classificação pelo material); integração em
`IntegrationTests/Projetos/PrecoPeloModelo3dTests.cs`, com o medidor de teste (`AmbienteDeTeste.Medidor`, sem SolidCraft).

**Sem migration na etapa 6:** a medição não é gravada no banco (fica em memória no servidor).

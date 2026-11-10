# Modelo 3D

Modelo 3D paramétrico da peça no SolidCraft. O joalheiro edita no SolidCraft embutido e a IA modela pelo MCP.

- **Código:** `modelo-3d`
- **Escopo:** versão e cada peça do projeto (`DaPeca`): a peça tem conversa, imagens e arquivo próprios.
- **Ferramentas da IA:** as do servidor MCP `solidcraft-cli mcp --headless` (execute, batch, set_parameter,
  inspect_design, measure, screenshot…). Configuração em `SolidCraftOptions` (seção `SolidCraft:Caminho`).
- **Arquivo de trabalho:** o projeto da versão em JSON (o mesmo que o SolidCraft web troca por
  `document.export`/`document.import`), com histórico e restauração.

| Camada | Pasta |
|---|---|
| Application | `Application/Ferramentas/Modelo3d` (contexto do turno, `MateriaisDoModelo`, `IMedidorDoModelo`, `PesoDoModelo`, `IEditorDoModelo`; commands `DefinirMaterialDosCorpos` e `UnirCorposSobrepostos`; query `GetConferenciaDoModelo`) |
| Infra | `Infra/IA/Ferramentas/Modelo3d` (instruções, MCP, texto dos passos, resumo do modelo, `MateriaisNoArquivo`, `MedidorDoModeloSolidCraft`, `EditorDoModeloSolidCraft`, `EdicaoNoArquivo`, `MontadorDoModeloSolidCraft`, `MontagemNoArquivo`) |
| REST | `Controllers/Ferramentas/Modelo3d/Modelo3dController` (`api/projetos/{id}/versoes/{versaoId}/modelo-3d`): conferência e `POST montar-a-partir-das-pecas` |
| GraphQL | `VersaoProjeto.conferenciaDoModelo` (`ConferenciaPorVersaoDataLoader`) |
| Front | `modules/ferramentas/modelo-3d` (`PonteSolidCraft` abre, salva e confere os materiais pelo iframe, com a API `window.solidcraft`; `cw-conferencia-modelo`; rotas da versão e da peça), `data/ferramentas/modelo-3d` |

Não tem estado de domínio próprio: usa o arquivo e a conversa do Assistente. Sem prévia em SVG na pasta da versão.
Cada arquivo novo ganha quatro imagens guardadas, pelo `screenshot` do SolidCraft em 1920x1200, sem grade nem eixos, nas
vistas `iso`, `front`, `top` e `right` (guardadas como `iso`, `frente`, `topo` e `lado`; ver
[Assistente](../../Assistente/README.md#imagens-guardadas)).

## Materiais do ateliê no arquivo (etapa 5 da precificação)

As ligas do ateliê viram **materiais dentro do arquivo 3D**, e a classificação de cada corpo (que liga, pedra ou apoio)
**vive no arquivo**, não no banco: acompanha o histórico do arquivo e o Desfazer.

- **Quais:** um material por **liga ativa**, com a densidade dela e uma aparência metálica coerente (ouro amarelo, branco,
  rosé, prata), mais **"Pedra"** (não entra no peso do metal; o custo da pedra vem da composição) e **"Apoio (não
  conta)"** (rascunho, guia ou corpo auxiliar, fora do cálculo). Montados em `MateriaisDoModelo` (Application).
- **Nome estável:** `Castwork liga {id}` (ex.: `Castwork liga 2`), o mesmo se o nome da liga mudar.
  `MateriaisDoModelo.LigaDoMaterial` faz o caminho de volta (a etapa 6 mede o peso por ele).
- **Quando:** ao abrir o arquivo da versão para um turno da IA, a Infra garante os materiais (`MateriaisNoArquivo`): lê o
  `material.list`, cria com `material.create` só os que faltam e, se a densidade da liga mudou no ateliê, atualiza com
  `material.edit`, tudo num `batch` (tudo ou nada). Abrir de novo não duplica nada. Nada é apagado: corpos podem usar o
  material de uma liga desativada. Se falhar, a IA modela assim mesmo (fica no log). O arquivo só é gravado se a IA
  também alterar o modelo; no turno seguinte os materiais são garantidos de novo.
- **Caminho genérico:** a ferramenta devolve no `PreparoDoTurno` o `PreparoDoArquivo` (a lista em JSON); o Assistente o
  leva no `PedidoAFerramenta` e a Infra aplica pelo `DefinicaoDeFerramenta.PrepararArquivo` (`Assistente/PreparoDoArquivo`).
- **IA:** o contexto da versão lista os `materiais` (nome, uso, densidade). As instruções pedem `material.assign` em todo
  corpo de metal que ela criar (no mesmo `batch`), pela liga do briefing/composição ou perguntando; "Pedra" e "Apoio
  (não conta)" quando for o caso; e não criar, editar nem apagar materiais.
- **"Modelo agora":** cada corpo sai com o material (`Aro (fechado, Castwork liga 2): …`); o "Default" do SolidCraft
  (1,29 g/cm³, plástico) conta como **sem material** e sai como `SEM MATERIAL`, com uma linha final listando esses corpos.
- **Livre para modelar:** nada impede criar corpo sem material.
- **Front:** a tela avisa "N corpos sem material" (discreto, sem bloquear) depois de abrir, salvar e trazer o que a IA fez,
  pelo `inspect.measure` do SolidCraft embutido (`window.solidcraft.request('command', …)`, em `corpos-sem-material.ts`).

Sem migration: nada muda no banco.

**Testes:** unitários em `UnitTests/Infra/IA/MateriaisNoArquivoTests.cs` (comandos a partir das ligas, nomes estáveis,
idempotência, densidade, instruções) e `ResultadoDaFerramentaTests.cs` (resumo com material). Integração em
`IntegrationTests/Ferramentas/Modelo3d/MateriaisDoModelo3dTests.cs`: o preparo do turno com as ligas ativas e, com o
SolidCraft de teste, materiais criados, sem duplicar, salvos no arquivo e lidos de volta pelo `measure`. Esse teste usa
o `SolidCraft:Caminho` dos User Secrets da API (ou `SolidCraft__Caminho`) e fica ignorado sem ele.

O front serve o SolidCraft web em `/solidcraft` (`npm run solidcraft:instalar`); o servidor precisa do
`solidcraft-cli` da mesma versão.

## Peso do metal medido no modelo (etapa 6 da precificação)

O preço da versão (`GetPrecoDaVersao`) usa o peso do metal **medido no arquivo 3D atual**, sem IA.

- **Abstração:** `IMedidorDoModelo` (Application): recebe o arquivo (`ArquivoParaMedir`: id e como ler o conteúdo) e os
  materiais do ateliê, e devolve os **corpos** (nome, material, volume em mm³, massa em g, fechado/aberto) e as
  **sobreposições** (pares e volume em comum). Nulo: não foi possível medir.
- **Classificação e soma:** `PesoDoModelo` acha o arquivo atual da conversa do Modelo 3D da versão (o mais recente não
  arquivado; em branco é "sem 3D"), manda medir e classifica cada corpo pelo material (`UsoDoCorpo`: liga, "Pedra",
  "Apoio (não conta)" ou sem material, que inclui o "Default"). O somatório por liga e as pendências/avisos ficam no
  DomainService (`ModeloMedido`, `CalculadoraDePreco`); ver [Precificação](../../Precificacao/README.md).
- **Infra:** `MedidorDoModeloSolidCraft` (`Infra/IA/Ferramentas/Modelo3d`): `solidcraft-cli mcp --headless` numa pasta
  temporária; abre o arquivo, **garante os materiais** (o mesmo preparo do turno da IA, `PreparoDoArquivo`), roda
  `measure` e `inspect.interference` (`budget_s` 20) e lê o resultado (`MedicaoNoArquivo`). Nada é gravado no arquivo.
- **Cache:** em memória (`IMemoryCache`, 1 dia sem uso), pela chave **id do arquivo + materiais** (hash do preparo).
  Cada alteração do arquivo é um id novo; mudar a densidade de uma liga muda o preparo. Abrir a tela de novo não mede de
  novo (medir leva ~1 s). O conteúdo do arquivo só é lido do banco quando a medição não está guardada. Falha não é
  guardada.
- **Falha não derruba a tela:** SolidCraft não configurado, arquivo que não abre, recusa ou tempo esgotado (30 s) viram
  log e o preço sai **Incompleto** com a pendência "Não foi possível medir o modelo 3D".

Sem migration: nada muda no banco.

**Testes:** unitários em `UnitTests/Infra/IA/MedicaoNoArquivoTests.cs` (leitura do `measure` e do
`inspect.interference`, cache pelo arquivo e pelos materiais). Integração com o SolidCraft de teste em
`IntegrationTests/Ferramentas/Modelo3d/MedidorDoModeloTests.cs`: um anel de ouro 18k com garra sobreposta, pedra e corpo
sem material, medido sem IA (2,824 g na liga, sobreposição Aro/Garra), sem medir de novo o mesmo arquivo e medindo de
novo quando a densidade muda (ignorado sem `SolidCraft:Caminho`). Nos outros testes de integração o medidor é o de
teste (`AmbienteDeTeste.Medidor`).

## Conferência de peso e materiais (etapa 7 da precificação)

Fecha o modelo híbrido: a IA atribui o material, o Castwork confere e o joalheiro corrige. A tela **Peso e materiais**
lista os corpos do arquivo 3D atual com material, volume e peso, destaca os sem material e deixa escolher ali mesmo; une
corpos de metal sobrepostos. Não cria nem apaga corpos (unir junta dois em um) e não mexe na forma (isso é no Modelo 3D).

- **Leitura (GraphQL):** `VersaoProjeto.conferenciaDoModelo` → `GetConferenciaDoModeloQuery` (`Regras.Projetos.Consultar`:
  peso e material não são dado de precificação). Usa a mesma medição do preço (`PesoDoModelo.MedirArquivoAtualAsync`,
  guardada pelo arquivo): `arquivoId` (nulo: sem 3D), `medido`, `alteravel` (versão em desenvolvimento), `pesoDoMetal` e
  `pesosPorLiga`, os `corpos` (nome, material, `UsoDoCorpo`, liga, volume, massa, fechado, `contaNoPeso`), as
  `sobreposicoes` (com `deMetal` e `podeUnir`: as duas da mesma liga) e os `materiais` para escolher (ligas ativas, Pedra e
  Apoio, com o rótulo da tela).
- **Commands (REST, `Regras.Projetos.Editar`):** `PUT .../modelo-3d/materiais` (`DefinirMaterialDosCorpos`: corpo →
  material, pelo nome no arquivo; só material do ateliê, sem tirar material) e `POST .../modelo-3d/unir-corpos`
  (`UnirCorposSobrepostos`: corpos de metal da **mesma liga** que se sobrepõem entre si; o primeiro dá o nome). Os dois
  levam o `arquivoId` que a tela mostrou e devolvem `ModeloAlteradoDto` (`arquivoId` novo e `arquivoAnteriorId`).
- **Caminho comum** (`AlteracaoNaConferencia`): versão publicada ou arquivada recusa (`Projeto.GarantirVersaoEmDesenvolvimento`,
  422); com a IA trabalhando no modelo, recusa; se o arquivo atual não é mais o que a tela mostrou, recusa ("o modelo 3D
  mudou: confira de novo"); corpo que não está na medição, recusa. O `IEditorDoModelo` aplica sem IA e o resultado vira
  **uma nova versão do arquivo** com a origem `OrigemArquivo.Conferencia` ("Conferência de peso e materiais" no histórico).
- **Desfazer:** a tela oferece Desfazer logo depois (restaura o `arquivoAnteriorId` pelo `RestaurarArquivoFerramenta`, como
  o histórico); depois, pelo histórico do modelo.
- **Infra:** `EditorDoModeloSolidCraft` (o mesmo `solidcraft-cli mcp --headless` da medição, numa pasta temporária): abre,
  garante os materiais do ateliê (`PreparoDoArquivo`), aplica `material.assign` (um por material, num `batch`, tudo ou
  nada) ou `solid.combine` (`join`), salva e devolve o conteúdo (`EdicaoNoArquivo` monta as chamadas). SolidCraft não
  configurado ou sem resposta: 503; recusa do SolidCraft: 422 com mensagem em português (o detalhe fica no log).
- **Cache da medição:** é pelo id do arquivo; o arquivo novo é medido de novo na próxima leitura (preço e conferência).
- **Front:** `cw-conferencia-modelo` abre por cima do Modelo 3D (botão "Peso e materiais", o aviso "N corpos sem material
  → Conferir" ou `?conferir=1`, que é o atalho do card Preço). Antes de abrir, o que foi modelado à mão é salvo. Aplicar,
  unir e desfazer recarregam o arquivo no SolidCraft à vista.

Sem migration: a nova origem do arquivo é um valor a mais no enum (coluna inteira).

**Testes:** integração em `IntegrationTests/Ferramentas/Modelo3d/ConferenciaDoModeloTests.cs` com o medidor e o editor de
teste (`AmbienteDeTeste.Editor`): leitura, nova versão do arquivo e desfazer, unir e as recusas (ligas diferentes, sem
sobreposição, corpo fora do modelo, material desconhecido, modelo que mudou), versão publicada/arquivada, permissões,
falha do SolidCraft e o caminho GraphQL + REST. Com o SolidCraft de teste, `EditorDoModeloTests.cs` (material e união de
verdade, medidos de novo; ignorado sem `SolidCraft:Caminho`). Unitários em `UnitTests/Infra/IA/EdicaoNoArquivoTests.cs`.

## Modelo 3D da peça do projeto

Cada peça do projeto tem o próprio modelo (rota `api/projetos/{id}/pecas/{pecaDoProjetoId}/ferramentas/modelo-3d/...`),
com conversa, imagens e histórico de arquivos só dela. O que vale como o modelo da peça é o **arquivo final**, marcado
pelo joalheiro no histórico (`MarcarArquivoFinalFerramentaCommand`, ver o README do Assistente).

- **Contexto da IA** (`ContextoDoModelo3d.MontarDaPecaAsync`): a peça (tipo, nome, liga e teor, `materialDaLiga`, peso
  informado, descrição, status), o briefing do projeto e os materiais do ateliê. As instruções pedem modelar só a peça,
  centrada na origem, com o material da liga dela.
- **Peso da peça** (`GetPesoDaPecaQuery`, `PesoDoModelo.MedirArquivoFinalDaPecaAsync` e `PesoDoModelo.PesoNaLiga`): o
  arquivo final é medido pelo mesmo medidor da versão (medição guardada pelo arquivo) e o peso é o **volume** dos corpos
  que contam (de metal e sem material; "Pedra", "Apoio" e corpo aberto ficam fora) × a **densidade da liga da peça**
  (atual, mesmo desativada). O material de cada corpo não muda o peso: a peça é de uma liga só. Sem 3D final: o digitado.
  Com 3D e sem liga (pedra, material livre): o digitado. Não foi possível medir: sem peso. GraphQL:
  `PecaDoProjeto.peso` (`PesoDaPeca`: `gramas`, `origem`, `pesoDigitado`), por `PesoPorPecaDoProjetoDataLoader`, e
  `Projeto.pecas(id:)` para pedir só uma peça.
- **Sem conferência** na peça (o peso não depende do material de cada corpo) e sem "Montar a partir das peças".
- **Nomes:** ao marcar o final (`Modelo3dNoAssistente`, `IArquivoFinalDaFerramenta`), o arquivo é medido e, se algum
  corpo não tem o nome da peça, o `IEditorDoModelo` renomeia (`RenomeacaoDeCorpos`, `body.rename` num `batch`): o corpo
  único fica "Elo Infinito 2,5 cm"; vários, "Nome", "Nome (2)"... (`NomesDosCorpos`). O renomeado vira uma nova versão do
  arquivo (`OrigemArquivo.NomesDaPeca`), que é a marcada. Sem medição (SolidCraft fora), marca como está.
- **Peça publicada:** trocar o arquivo final faz dela outra peça; salvar, a IA, desfazer e restaurar sem marcar não.
  Publicar leva o modelo final para a biblioteca (sem final, o 3D não vai); importar traz uma cópia, já marcada como final.

## Montar a partir das peças (versão)

O modelo da versão é só a montagem das peças: ele é montado a partir do modelo 3D **final** de cada peça da composição (`MontarAPartirDasPecasCommand`, `POST
.../modelo-3d/montar-a-partir-das-pecas`, `projetos.editar`): uma cópia do modelo de cada peça **por unidade** (a
quantidade da composição), enfileiradas ao longo do X depois do que o modelo já tem, para a IA ou o joalheiro
posicionarem depois. Grava uma nova versão do arquivo (`OrigemArquivo.Montagem`, desfazível pelo histórico) e devolve as
peças montadas e as que ficaram de fora por não terem modelo 3D final.

- **Recusa:** versão publicada ou arquivada; a IA trabalhando no modelo; nenhuma peça da composição com modelo final; mais de
  100 unidades.
- **Infra** (`MontadorDoModeloSolidCraft`, `MontagemNoArquivo`), o mesmo `solidcraft-cli mcp --headless` da medição, numa
  pasta temporária: mede a faixa em X de cada peça (`measure`, bbox), abre o arquivo da versão (ou começa um novo),
  garante os materiais do ateliê, e para cada unidade `component.insert_design` (o arquivo da peça, deslocado em X, num
  componente "Elo oval (3)") seguido de `component.break_link` (o componente fica com os corpos e esquece o arquivo) e do
  `body.rename` que dá aos corpos da unidade o nome dela ("Elo oval (3)"; vários: "Elo oval (3) (2)"...). Os
  corpos mantêm o material do arquivo da peça; o material que o SolidCraft renomeou por ter outra densidade no arquivo da
  peça (`Castwork liga 2 (linked 1)`) volta ao do ateliê e o corpo sem material recebe a liga da peça (`material.assign`
  num `batch`). Viabilidade conferida no SolidCraft `73e188d`: os corpos chegam com o material e o arquivo salvo não
  depende mais do da peça.
- **IA da versão:** o contexto marca na composição quais peças têm modelo final (`temModelo3d`), e as instruções dizem
  que a IA só monta e posiciona os componentes das peças, sem criar corpos; faltando uma peça, ela orienta modelá-la na
  própria peça e marcar o final. O backend também recusa: na conversa da versão (`PedidoAFerramenta.EscopoDaVersao`), a
  `DefinicaoDeFerramenta.RecusarNaVersao` do modelo 3D (`CriacaoDeCorpos`) devolve erro à IA, sem chamar o MCP, em
  `execute` ou `batch` com um comando que cria corpos (`solid.extrude`, primitivas, superfícies, chapas, malhas,
  `component.insert_design`...). Na peça, o 3D continua livre.

**Testes:** integração em `IntegrationTests/Projetos/DesenhoDasPecasTests.cs` (peso da peça, montagem com o montador de
teste `AmbienteDeTeste.Montador`, recusas) e, com o SolidCraft de teste, `Ferramentas/Modelo3d/MontadorDoModeloTests.cs`
(duas unidades do elo e a pedra, materiais do ateliê, sem sobreposição; ignorado sem `SolidCraft:Caminho`). O mesmo arquivo confere, no
SolidCraft, o `body.rename` do arquivo final. Unitários em `UnitTests/Infra/IA/MontagemNoArquivoTests.cs`,
`CriacaoDeCorposTests.cs`, `EdicaoNoArquivoTests.cs`, `NomesDosCorposTests.cs` e `PesoDoModeloTests.cs` (peso na liga).

## Instalação

A ponte com o editor usa `document.export`, `document.import` e `document.saved`, que **não existem na release
0.30.1** (a última publicada quando a ferramenta entrou). Até sair uma release com elas, use um build da `main`
do [SolidCraft](https://github.com/tangency-labs/solidcraft). Testado com o commit `73e188d` (0.40.0-rc.2).

1. Pré-requisitos: Rust (`rustup`, com o alvo `wasm32-unknown-unknown`) e [Trunk](https://trunkrs.dev).
2. Build, na pasta do SolidCraft, no commit fixado:
   ```bash
   git checkout 73e188d
   cargo build --release --bin solidcraft-cli
   (cd apps/solidcraft-web && trunk build --release --public-url ./)
   ```
3. Servidor: aponte `SolidCraft:Caminho` (User Secrets ou variável `SolidCraft__Caminho`) para
   `target/release/solidcraft-cli`. Na subida, `VerificacaoSolidCraft` avisa se o caminho não existe.
4. Front: copie o editor web com
   `SOLIDCRAFT_DIST=<pasta-do-solidcraft>/apps/solidcraft-web/dist npm run solidcraft:instalar`.
   `public/solidcraft` fica fora do git; refaça este passo em cada máquina e no build de produção.

CLI e editor web precisam ser do **mesmo commit**: o arquivo JSON (`solidcraft/51`) passa de um para o outro.
Ao atualizar o SolidCraft, troque o commit acima e confira que abrir, salvar e a conversa com a IA seguem funcionando.

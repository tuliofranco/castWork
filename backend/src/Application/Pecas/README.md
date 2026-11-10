# Peças

As peças (elo, fecho, pedra, corrente comprada pronta) existem em dois lugares:

- **Peça do projeto** (`PecaDoProjeto`, dentro do agregado `Projeto`): é a que entra na composição das versões. Nasce no
  projeto como **rascunho** ou é **importada** da biblioteca (cópia **publicada**, vinculada à original). Só ela tem
  **valor**: o custo de uma unidade **naquele projeto** (`Dinheiro?`, compra ou produção, não o preço de venda). A mesma
  peça pode ter valores diferentes em projetos diferentes; peça sem valor deixa o preço da versão incompleto (pendência
  `PecaSemValor`).
- **Biblioteca** (`Peca`): só consulta pela tela, **sem valor**. Só ganha peças pela **publicação** de uma peça do
  projeto. Mostra o **histórico de uso**: os projetos (e versões) que usam a peça pelas cópias vinculadas, com o valor
  em cada projeto. Peça em uso não é excluída, é desativada.

## Material: liga do ateliê ou texto livre

| Tipo | Material |
|---|---|
| Todos menos **Pedra** | Uma **liga ativa** do ateliê (`Liga`), opcional. A peça guarda o `LigaId` e uma **cópia** do nome (`Material`) e do teor (`Teor`) da liga no momento da escolha |
| **Pedra** | Texto livre (`Material`, ex.: "Turmalina"), sem liga e sem teor |

- A regra fica em `MaterialDaPeca.Escolher` (Domain), usada pela `Peca` e pela `PecaDoProjeto`. Liga na pedra e texto
  livre nos outros tipos são recusados (validator e domínio).
- **Cópia:** editar ou desativar a liga depois não muda a peça. Salvar com a **mesma liga** já gravada mantém a cópia,
  ainda que a liga tenha mudado ou esteja desativada; trocar para outra liga exige que ela esteja ativa (422). Na peça
  nova, a liga precisa estar ativa (400, "Liga não encontrada ou desativada.").
- **Publicar** leva para a biblioteca o `LigaId` e a cópia como estão; **importar** traz o mesmo de volta.
- **Trocar a liga** de uma peça publicada é alterar os dados: ela vira outra peça (abaixo).
- Peças anteriores à liga (migration `LigaDasPecas`): ficaram com o texto do material e o teor como cópia e `LigaId`
  nulo; a liga não foi adivinhada pelo texto. Salvar uma delas sem escolher liga mantém esse texto.
- As leituras (GraphQL, detalhe da biblioteca, lista do projeto, composição) mostram o `material`: o nome copiado da
  liga ou o material livre da pedra.

## Regras da peça do projeto

| Ação | Efeito |
|---|---|
| Criar | Rascunho, sem vínculo e sem valor |
| Importar da biblioteca | Copia os dados (inclusive o `LigaId` e a cópia da liga) e o desenho (cada `DesenhoDaPeca` vira a conversa da ferramenta na peça do projeto, com o arquivo de origem `Biblioteca` já marcado como final, o que não conta como alteração); publicada, vinculada à peça da biblioteca, sem valor. A mesma peça não é importada duas vezes no projeto (a cópia que virou outra peça não conta); peça desativada da biblioteca não é importada |
| Definir valor | Só o valor (nulo tira); não muda status nem vínculo |
| Alterar dados | Se algum dado (nome, tipo, liga ou material livre, peso, descrição) mudou numa peça publicada, ela vira **outra peça**: volta a rascunho e perde o vínculo; a peça da biblioteca não muda. Repetir os mesmos dados (espaços e zeros à direita não contam) ou só ativar/desativar não muda nada disso. O valor fica |
| Publicar | Só rascunho e ativa. Cria a `Peca` na biblioteca com os dados (sem valor; o `LigaId` e a cópia da liga como estão) e o **desenho** (o arquivo final do desenho 2D e do modelo 3D da peça, em `DesenhoDaPeca`, com as imagens guardadas; a ferramenta sem final não vai) e vincula, no mesmo `SaveChanges` (navegação privada `PecaPublicada`, só para o EF) |
| Alterar o desenho | Trocar o **arquivo final** do desenho 2D ou do modelo 3D da peça (`MarcarArquivoFinalFerramentaCommand`) numa peça publicada faz dela **outra peça**, como alterar os dados (`Projeto.AlterarDesenhoDaPeca`). Salvar um arquivo novo sem marcá-lo (o joalheiro ou a IA, desfazer, restaurar) não muda o final nem a peça. A peça da biblioteca e o desenho dela não mudam |
| Composição | `ItemComposicao.PecaDoProjetoId`: só peças **ativas do mesmo projeto**. Duplicar a versão aponta para as mesmas peças |
| Publicar a versão | Recusa (422) se alguma peça da composição está em rascunho, com os nomes delas na mensagem |

**Desenho e peso da peça do projeto:** cada peça tem o próprio desenho 2D e modelo 3D (as ferramentas do Assistente com
`PecaDoProjetoId`; ver [Desenho 2D](../Ferramentas/Desenho2d/README.md) e [Modelo 3D](../Ferramentas/Modelo3d/README.md)).
O desenho é o **arquivo final** de cada ferramenta (GraphQL `PecaDoProjeto.arquivosFinais`). Com modelo 3D final, o peso
da peça é o medido nele, na liga da peça (`GetPesoDaPecaQuery`, GraphQL `PecaDoProjeto.peso`); sem 3D final, o digitado. O preço da versão não usa o peso das peças (a composição entra pelo valor no projeto): ele aparece no peso
total da composição, na tela da versão.

**Ficha técnica da peça do projeto:** `GET api/projetos/{id}/pecas/{pecaDoProjetoId}/documentos/ficha-tecnica.pdf`
(`GetFichaTecnicaDaPecaQuery`, `projetos.ler`) gera o PDF na hora, sem guardar, pelo `IGeradorFichaTecnica` (WordCraft,
o mesmo visual do briefing: marca do ateliê no topo e seções). Leva nome, tipo, status, liga e teor (ou o material da
pedra), o peso com a origem (reaproveita o `GetPesoDaPecaQuery`), a descrição, o projeto (e o cliente), o resumo do
briefing quando existe e a data de emissão. O **valor no projeto** só entra para quem tem `precificacao.ler`. Leva também,
na seção "Desenho", as **imagens guardadas** do arquivo final (a iso do modelo 3D e o PNG do desenho 2D), quando já
existem; a prévia sob demanda não entra.

**Imagens do desenho:** cada arquivo novo do 2D e do 3D ganha imagens em alta qualidade em segundo plano (ver
[Assistente](../Assistente/README.md#imagens-guardadas)). Publicar copia as do arquivo final para a peça da biblioteca
(`ImagemDoDesenhoDaPeca`, tabela `peca_desenho_imagens`, filha do `DesenhoDaPeca`) e importar as copia de volta para o
arquivo novo do projeto. Leitura da biblioteca: `GET api/pecas/{id}/desenhos/{ferramenta}/imagens` (vistas) e
`.../imagens/{vista}?formato=png|svg` (binário), queries `GetImagensDoDesenhoDaPecaQuery` e `GetImagemDoDesenhoDaPecaQuery`.

Não se exclui peça do projeto (a que está numa composição só pode ser desativada). A peça publicada na biblioteca nunca é
alterada pelo projeto: alterar a cópia cria outra peça.

| Camada | Onde |
|---|---|
| Domain | `Domain/Entities/Pecas` (`Peca`, `TipoPeca`, `MaterialDaPeca`); `Domain/Entities/Projetos` (`PecaDoProjeto`, `StatusPecaDoProjeto`; ações em `Projeto.CriarPeca`, `ImportarPeca`, `AlterarPeca`, `DefinirValorDaPeca`, `PublicarPeca`) |
| Application | `Application/Pecas`: commands `CriarPeca` e `AlterarPeca` da biblioteca (campos comuns em `IDadosDaPeca`: `LigaId` ou, na pedra, `Material`; validação em `ValidarDadosDaPeca`, que só aceita liga ativa na peça nova); projeção `ParaHistoricoDeUso` (`UsoDaPecaDto`). `Application/Projetos/Commands`: `CriarPecaDoProjeto`, `ImportarPecaDaBiblioteca`, `AlterarPecaDoProjeto`, `DefinirValorDaPecaDoProjeto` e `PublicarPecaDoProjeto` |
| Infra | Tabelas `pecas` e `projeto_pecas` com `liga_id` (FK restrict para `ligas`), `material` e `teor` (a cópia). Tabela `pecas` (sem valor); tabela `projeto_pecas` (`valor` numeric(18,2) nulo, `status`, `peca_id` FK restrict para `pecas`); `projeto_versao_itens.peca_do_projeto_id` (FK NO ACTION para `projeto_pecas`). Migrations `PecasDoProjeto` e `LigaDasPecas` |
| REST | Corpo da peça: `nome`, `tipo`, `ligaId` (todo tipo menos pedra) ou `material` (só pedra), `pesoGramas`, `descricao`. `PecasController` (`api/pecas`): `POST` e `PUT {id}` da biblioteca continuam, mas a tela não os usa. `PecasDoProjetoController` (`api/projetos/{id}/pecas`): `POST` cria, `POST importar` (`{ pecaId }`), `PUT {pecaDoProjetoId}` altera os dados (com `ativo`), `PUT {pecaDoProjetoId}/valor` (`{ valor }`), `POST {pecaDoProjetoId}/publicar` (devolve o Id da peça nova da biblioteca), `GET {pecaDoProjetoId}/documentos/ficha-tecnica.pdf` (ficha técnica em PDF, gerada na hora) |
| Biblioteca: desenho | `DesenhoDaPeca` (filho do agregado `Peca`, tabela `peca_desenhos`: ferramenta, conteúdo e quando foi copiado; um por ferramenta). GraphQL `Peca.desenhos` (sem o conteúdo, por `DesenhosPorPecaDataLoader`); conteúdo em `GET api/pecas/{id}/desenhos/{ferramenta}` (`GetDesenhoDaPecaQuery`, `pecas.ler`; 204 sem desenho). Front: o detalhe mostra os cartões e abre o desenho **só para ver** em `pecas/:id/desenhos/:ferramenta` (CADCraft ou SolidCraft embutido, nada é gravado) |
| GraphQL | `Peca` e `PecaDoProjeto` têm `ligaId`, `material` (nome copiado da liga ou material livre) e `teor` (copiado). `ligas` (ativas, `projetos.ler`) alimenta o seletor. `PecasQueries`: `pecas` (paginado) e `peca(id)`, com `pecas.ler`; `Peca.historicoDeUso` (`UsoDaPeca`, por DataLoader) com `projetos.ler`, e o `valor` de cada uso com `precificacao.ler`. `Projeto.pecas` (`PecaDoProjeto`, por DataLoader, por nome) com `status`, `pecaId` e `valor` (este com `precificacao.ler`). `ItemComposicao.peca` é a `PecaDoProjeto` |
| Front | `modules/pecas` (biblioteca só de busca e detalhe de consulta com o histórico); seção **Peças do projeto** na página do projeto (`modules/projetos`); `data/pecas/pecas` e `data/projetos/projetos` (serviços) |

**Permissões (regra exata):**

- Criar, importar, alterar os dados e publicar a peça do projeto: `Regras.Projetos.Editar` (`projetos.editar`). Na tela,
  a busca do "Importar da biblioteca" lê a biblioteca, então pede também `pecas.ler`.
- Definir o valor: `Regras.Precificacao.Editar` (`precificacao.editar`). Ver o valor (`PecaDoProjeto.valor` e
  `UsoDaPeca.valor`): `precificacao.ler`; sem a regra, o campo responde erro de autorização e o valor não sai.
- Biblioteca: consultar com `Regras.Pecas.Consultar`; o histórico de uso diz em que projetos a peça está, então exige
  também `projetos.ler` (sem a regra, só o campo `historicoDeUso` vem nulo, com o erro). `POST`/`PUT api/pecas` seguem
  com `Regras.Pecas.Criar` e `Regras.Pecas.Alterar`.

**Migration `DesenhoDasPecas`:** `ferramenta_conversas.peca_do_projeto_id` (FK cascade para `projeto_pecas`; o índice
único passa a ser projeto + versão + peça + ferramenta, com nulos iguais) e a tabela `peca_desenhos`. As conversas que
já existiam ficam do projeto ou da versão; as peças que já estavam na biblioteca ficam sem desenho. O `Down` apaga as
conversas das peças. Teste com dados antigos em `IntegrationTests/Projetos/DesenhoDasPecasTests.cs`.

**Migration `ArquivoFinalDasPecas`:** `ferramenta_conversas.arquivo_final_id` (FK set null para `ferramenta_arquivos`).
Cada conversa de peça que já tinha desenho fica com o arquivo atual (o mais recente não arquivado) como final; se ele era
o desenho em branco, sem final. Conversas da versão e do projeto ficam sem final. Teste com dados antigos no mesmo arquivo.

**Migration `PecasDoProjeto`:** para cada projeto, cada peça da biblioteca usada em alguma versão dele virou uma peça do
projeto (uma por projeto, a mesma em todas as versões), publicada, vinculada à original e com o **custo atual** dela como
valor; os itens passaram a apontar para essas peças (coluna nova `peca_do_projeto_id`, para não repetir por um instante
o par do índice único) e a coluna `pecas.custo` saiu. As peças da biblioteca continuam lá (contam como publicadas) e as
versões já publicadas não são conferidas de novo. O `Down` volta ao possível: o custo da peça da biblioteca é o valor da
cópia mais recente que tem valor, e itens de peças em rascunho (sem peça na biblioteca) se perdem.

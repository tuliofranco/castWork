# Peças

Biblioteca de peças: componentes reutilizáveis (elo, fecho, pedra, corrente comprada pronta). A mesma peça entra em
versões de projetos diferentes. Peça em uso não é excluída, é desativada.

- **Custo por unidade** (`Peca.Custo`, `Dinheiro?`): custo de **compra ou produção** de uma unidade, não o preço de
  venda. É opcional: peça sem custo continua válida, mas deixa o preço da versão incompleto (etapa 4 de
  [`docs/PRECIFICACAO.md`](../../../../docs/PRECIFICACAO.md)). Pedra não é precificada pelo volume do 3D: o custo dela vem
  daqui. Nunca negativo (o `Dinheiro` garante); custo por quilate ou tamanho e histórico de custos ficam para depois.

| Camada | Onde |
|---|---|
| Domain | `Domain/Entities/Pecas` (`Peca`, `TipoPeca`); `Peca.DefinirCusto` separado de `Peca.Atualizar` |
| Application | `Application/Pecas`: commands `CriarPeca` e `AlterarPeca` (campos comuns em `DadosDaPeca` e `ValidarDadosDaPeca`) |
| Infra | `Infra/Data/Configurations/Pecas` (tabela `pecas`, coluna `custo` numeric(18,2) nula: migration `CustoDasPecas`) |
| REST | `PecasController` (`api/pecas`): `POST` cadastra, `PUT {id}` altera uma peça ativa |
| GraphQL | `PecasQueries`: `pecas` (paginado) e `peca(id)`, com a policy `pecas.ler`; campo `custo` do `PecaType` com `precificacao.ler` |
| Front | `modules/pecas` (lista, `nova` e `:id/editar`) e `data/pecas/pecas` (serviço) |

**Permissões:** consultar com `Regras.Pecas.Consultar`; cadastrar com `Regras.Pecas.Criar`; alterar com
`Regras.Pecas.Alterar`. O **custo** é dado de precificação: o campo `custo` do GraphQL exige `precificacao.ler`
(sem a regra, responde erro de autorização e o valor não sai). Nos commands, o custo só é gravado para quem tem
`precificacao.ler`; para os demais é ignorado (no cadastro fica sem custo, na alteração o atual é mantido), porque quem
não vê o custo não deve apagá-lo sem querer.

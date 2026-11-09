# Assistente (subdomínio genérico)

A conversa com a IA que toda ferramenta usa. Não conhece nenhuma ferramenta pelo nome: recebe o código
da ferramenta pela rota e pergunta ao `CatalogoDeFerramentas` como prepará-la.

## O que faz
- Conversa por ferramenta, com vários chats (novo chat, histórico, continuar um chat antigo).
- Turno ao vivo: pensamento, passos e consumo de tokens chegam ao front por SSE.
- Ações da IA ficam registradas e podem ser desfeitas; o arquivo de trabalho tem histórico e restauração.
- Imagens anexadas às mensagens.

## Onde está em cada camada
| Camada | Pasta |
|---|---|
| Domain | `Domain/Entities/Assistente` (ConversaFerramenta, MensagemFerramenta, ArquivoFerramenta, AcaoDaIa…) |
| Application | `Application/Assistente` (commands, queries, `IFerramentaComIa`, catálogo) |
| Infra | `Infra/IA/Assistente` (Claude, cliente MCP, painel do turno) |
| Presentation | `Presentation/WebAPI/Controllers/Assistente` |
| Front | `modules/assistente` (`cw-conversa-ia`), `shared/ui/ferramenta-ia`, `data/assistente` |

## Rotas
- Ferramenta da versão: `api/projetos/{projetoId}/versoes/{versaoId}/ferramentas/{ferramenta}/…`
- Ferramenta do projeto: `api/projetos/{projetoId}/ferramentas/{ferramenta}/…`

O escopo vem da rota. A validação recusa uma ferramenta do projeto chamada pela versão e vice-versa.

## Regra
O Assistente nunca depende de `Ferramentas.*`. Isso é garantido por
`test/ArchTests/ContextosDasFerramentasTests.cs`.

## Anexos "Do projeto"
Além das imagens, a fala pode levar até 3 arquivos do projeto (`ConversarFerramentaCommand.Anexos`). Quem
oferece arquivos implementa `IFonteDeAnexos` (registrada por varredura): o Assistente traz os arquivos atuais
das ferramentas (`arquivo`) e o Briefing, o último PDF (`briefing`). A mensagem guarda só a referência
(`AnexoDaMensagem`); a Infra grava o arquivo na pasta do turno (ferramentas com MCP) ou manda o PDF como
documento, só naquele turno.

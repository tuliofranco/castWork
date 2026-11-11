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
- Ferramenta da peça do projeto: `api/projetos/{projetoId}/pecas/{pecaDoProjetoId}/ferramentas/{ferramenta}/…`
- Ferramenta do projeto: `api/projetos/{projetoId}/ferramentas/{ferramenta}/…`

O escopo vem da rota (`IEscopoDaFerramenta`, nos commands por `ComandoDeFerramenta` e nas queries; `ConversaDoEscopo`
acha a conversa). A validação recusa uma ferramenta do projeto chamada pela versão ou pela peça, uma ferramenta da versão
chamada pelo projeto, uma ferramenta que não é da peça (`IFerramentaComIa.DaPeca`) chamada pela peça e versão e peça
juntas.

## Desenho da peça do projeto
Cada peça tem uma conversa só por ferramenta (sem "Novo chat": `ConversaFerramenta.NovoChat` recusa na peça) e um
arquivo só, com o histórico das versões dele. O desenho da peça é o **arquivo final** (`ConversaFerramenta.ArquivoFinalId`,
coluna `ferramenta_conversas.arquivo_final_id`, FK set null para `ferramenta_arquivos`), escolhido pelo joalheiro no
histórico: `MarcarArquivoFinalFerramentaCommand` (`POST …/arquivo/historico/{arquivoId}/final`). A ferramenta pode ajustar
o arquivo antes (`IArquivoFinalDaFerramenta`: no modelo 3D, os corpos recebem o nome da peça, numa nova versão do arquivo
com origem `NomesDaPeca`, que é a marcada). Trocar o final chama `Projeto.AlterarDesenhoDaPeca`: a peça publicada vira
outra peça. Salvar, a IA, desfazer, restaurar, renomear e desarquivar só mudam o arquivo atual e não contam. O final não
arquiva (`GarantirQuePodeArquivar`). Peso, montagem na versão, publicação, ficha técnica e o visualizador usam só o final:
`ConversaDoEscopo.ArquivoFinalAsync`, e as leituras do arquivo, da prévia e das imagens com `?final=true`. Sem final, a
peça não tem desenho naquela ferramenta. A lista dos finais de cada peça sai no GraphQL (`PecaDoProjeto.arquivosFinais`).

## Imagens guardadas
Todo arquivo novo das ferramentas (salvar, a IA, desfazer, restaurar, conferência, montagem, importação) entra na
`IFilaDeImagens` depois de ir para o banco (`ImagensDoArquivoInterceptor`, no EF: nenhum caminho fica de fora; o desenho
em branco não entra). Em segundo plano (`ImagensEmSegundoPlano`, um por vez, fila em memória), o
`GerarImagensDoArquivoCommand` pede ao `IGeradorDeImagens` (Infra: `GeradorDeImagensMcp`, pelo programa da ferramenta, sem
IA, com as `VistasGuardadas` da `DefinicaoDeFerramenta`) e grava `ImagemDoArquivo` (tabela `ferramenta_arquivo_imagens`:
arquivo, vista `iso`/`frente`/`topo`/`lado`/`2d`, tipo PNG ou SVG, conteúdo; uma por vista e tipo). Falha vai para o log e
a fila segue; a resposta do salvar não espera. Arquivo que já tem imagens (copiadas da biblioteca ao importar) não é
gerado de novo. Se a API reinicia com a fila cheia, aqueles arquivos ficam sem imagens.

Leitura (arquivo atual do escopo): `GET …/arquivo/imagens` (`GetImagensDoArquivoQuery`: vistas e tipos, na ordem de
mostrar) e `GET …/arquivo/imagens/{vista}?formato=png|svg` (`GetImagemDoArquivoQuery`; sem formato, o SVG quando existe).
Sem imagem guardada, a tela usa a prévia sob demanda (`…/arquivo/previa`), que continua igual.

## Regra
O Assistente nunca depende de `Ferramentas.*`. Isso é garantido por
`test/ArchTests/ContextosDasFerramentasTests.cs`.

## Anexos "Do projeto"
Além das imagens, a fala pode levar até 3 arquivos do projeto (`ConversarFerramentaCommand.Anexos`). Quem
oferece arquivos implementa `IFonteDeAnexos` (registrada por varredura): o Assistente traz os arquivos atuais
das ferramentas (`arquivo`) e o Briefing, o último PDF (`briefing`). A mensagem guarda só a referência
(`AnexoDaMensagem`); a Infra grava o arquivo na pasta do turno (ferramentas com MCP) ou manda o PDF como
documento, só naquele turno.

## Preparo do arquivo
Uma ferramenta pode pedir que o arquivo de trabalho tenha algo ao ser aberto (`PreparoDoTurno.PreparoDoArquivo`, JSON que
só ela entende). O Assistente leva o preparo no `PedidoAFerramenta`; a Infra, logo depois de abrir o arquivo, aplica o
`DefinicaoDeFerramenta.PrepararArquivo` da ferramenta (lê o arquivo e monta só o que falta). Não conta como alteração da
IA. Hoje: os materiais do ateliê no modelo 3D ([Modelo 3D](../Ferramentas/Modelo3d/README.md)).

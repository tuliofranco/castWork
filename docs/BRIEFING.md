# Briefing do projeto

Modelo aprovado em 05/11/2026. O Briefing define **o que será feito** no projeto, acima das versões
(cada versão é um jeito de fazer). É montado por conversa com uma IA e entregue como PDF.

## Fluxo

1. **Formulário inicial** (ao abrir o Briefing pela primeira vez), poucos campos obrigatórios:

   | Campo | Tipo | Obrigatório |
   |---|---|---|
   | Tipo de peça | anel, pulseira, colar, brinco, pingente, outro | sim |
   | O que o cliente quer | texto curto | sim |
   | Ocasião / para quem | texto curto | não |
   | Materiais pretendidos | escolha múltipla (ouro amarelo, ouro branco, prata, pedras…) | não |
   | Medidas conhecidas | texto curto | não |
   | Prazo de entrega | data | não |
   | Orçamento | valor ou faixa | não |

   O cliente vem do projeto.

2. **Conversa com a IA, já contextualizada.** A IA resume o que entendeu do formulário e pergunta
   só o que falta ou está ambíguo. Ao lado do chat, o resumo do Briefing é atualizado a cada
   resposta. Corrigir o formulário avisa a IA na conversa. A conversa fica salva no projeto.

3. **Gerar PDF**, pelo botão ou pedindo no chat. Cada PDF fica guardado com data e hora (o último
   em destaque) e pode ser baixado. Dá para continuar a conversa e gerar outro.

## Regras

- O joalheiro não escreve no documento: tudo passa pelo formulário e pela conversa.
- Um único PDF serve para a bancada e para o cliente.
- Nesta etapa a IA só cuida do Briefing (não sugere peças nem cria versões).
- Layout do PDF segue o `DESIGN.md` (papel claro, Fraunces nos títulos, nome do ateliê no topo).

## Arquitetura

- A IA roda **só no backend** (Claude, chave em User Secrets), com as permissões do app.
- Ferramentas da IA: ler e gravar o Briefing do projeto; montar o documento no **WordCraft**
  (`wordcraft-cli mcp`, via SDK `ModelContextProtocol` de C#), preso à pasta do projeto.
- Fonte da verdade: o Briefing estruturado no Postgres; `.docx` e PDF são gerados a partir dele.
- Plano B se o WordCraft (alpha) falhar: `wordcraft-cli convert` a partir de Markdown, sem MCP.

## Fora desta entrega

Envio por e-mail (botão previsto, desligado), edição à mão do documento, fotos de referência,
croqui, histórico de alterações.

## Riscos

WordCraft em alpha; dados do cliente enviados à API da Claude (LGPD: mandar só o necessário);
custo por conversa; binário do WordCraft na imagem Docker de produção.

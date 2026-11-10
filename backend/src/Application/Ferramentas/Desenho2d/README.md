# Desenho 2D

Desenho técnico da peça no CADCraft. O joalheiro edita no CADCraft embutido e a IA desenha pelo MCP.

- **Código:** `desenho-2d`
- **Escopo:** versão e cada peça do projeto (`DaPeca`): a peça tem conversa, imagens e arquivo próprios.
- **Ferramentas da IA:** as do servidor MCP `cadcraft-cli mcp` (command_line, execute, render, inspect_drawing…).
  Configuração em `CadCraftOptions`.
- **Arquivo de trabalho:** o DXF da versão (ou da peça), com histórico e restauração.
- **Contexto da IA:** na versão, o projeto, a versão com a composição e o briefing; na peça, a peça (tipo, nome, liga e
  teor, peso informado, descrição, status) e o briefing do projeto (`ContextoDoDesenho2d.MontarDaPecaAsync`). As
  instruções dizem que, com "peca" no contexto, a IA desenha só aquela peça.
- **Arquivo final:** na peça, o desenho é o arquivo marcado como final no histórico (ver [Assistente](../../Assistente/README.md#desenho-da-peça-do-projeto)).
  Numa peça publicada, trocar o final faz dela outra peça (ver [Peças](../../Pecas/README.md)); salvar sem marcar não.
  Publicar leva o DXF final para a biblioteca; importar traz uma cópia, já final.

Cada arquivo novo ganha a imagem guardada `2d` em dois tipos: o SVG (o `save` com extensão `.svg`, vetorial, que a tela
mostra com zoom) e um PNG de 2400x1500 (o `render` com `fit`, para a ficha técnica). O `render` do CADCraft não tem opção de
fundo: os dois saem em fundo branco, e a tela troca as cores do SVG para o fundo escuro (`emFundoEscuro`). Ver
[Assistente](../../Assistente/README.md#imagens-guardadas).

| Camada | Pasta |
|---|---|
| Application | `Application/Ferramentas/Desenho2d` (contexto do turno) |
| Infra | `Infra/IA/Ferramentas/Desenho2d` (instruções, MCP, texto dos passos) |
| Front | `modules/ferramentas/desenho-2d` (`PonteCadCraft` abre e salva pelo iframe); rotas `projetos/:id/versoes/:versaoId/desenho-2d` e `projetos/:id/pecas/:pecaId/desenho-2d` |

Não tem estado de domínio próprio: usa o arquivo e a conversa do Assistente.

**Testes:** o escopo da peça (conversa e arquivo separados da versão e das outras peças, contexto da peça, a peça
publicada que vira outra) em `IntegrationTests/Projetos/DesenhoDasPecasTests.cs`.

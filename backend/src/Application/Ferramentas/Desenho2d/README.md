# Desenho 2D

Desenho técnico da peça no CADCraft. O joalheiro edita no CADCraft embutido e a IA desenha pelo MCP.

- **Código:** `desenho-2d`
- **Escopo:** versão.
- **Ferramentas da IA:** as do servidor MCP `cadcraft-cli mcp` (command_line, execute, render, inspect_drawing…).
  Configuração em `CadCraftOptions`.
- **Arquivo de trabalho:** o DXF da versão, com histórico e restauração.

| Camada | Pasta |
|---|---|
| Application | `Application/Ferramentas/Desenho2d` (contexto do turno) |
| Infra | `Infra/IA/Ferramentas/Desenho2d` (instruções, MCP, texto dos passos) |
| Front | `modules/ferramentas/desenho-2d` (`PonteCadCraft` abre e salva pelo iframe) |

Não tem estado de domínio próprio: usa o arquivo e a conversa do Assistente.

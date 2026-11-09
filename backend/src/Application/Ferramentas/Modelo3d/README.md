# Modelo 3D

Modelo 3D paramétrico da peça no SolidCraft. O joalheiro edita no SolidCraft embutido e a IA modela pelo MCP.

- **Código:** `modelo-3d`
- **Escopo:** versão.
- **Ferramentas da IA:** as do servidor MCP `solidcraft-cli mcp --headless` (execute, batch, set_parameter,
  inspect_design, measure, screenshot…). Configuração em `SolidCraftOptions` (seção `SolidCraft:Caminho`).
- **Arquivo de trabalho:** o projeto da versão em JSON (o mesmo que o SolidCraft web troca por
  `document.export`/`document.import`), com histórico e restauração.

| Camada | Pasta |
|---|---|
| Application | `Application/Ferramentas/Modelo3d` (contexto do turno) |
| Infra | `Infra/IA/Ferramentas/Modelo3d` (instruções, MCP, texto dos passos) |
| Front | `modules/ferramentas/modelo-3d` (`PonteSolidCraft` abre e salva pelo iframe, com a API `window.solidcraft`) |

Não tem estado de domínio próprio: usa o arquivo e a conversa do Assistente. Sem prévia em SVG na pasta da versão.

O front serve o SolidCraft web em `/solidcraft` (`npm run solidcraft:instalar`); o servidor precisa do
`solidcraft-cli` da mesma release.

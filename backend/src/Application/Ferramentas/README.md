# Ferramentas

Cada pasta é um contexto próprio (bounded context). Uma ferramenta não conhece outra; todas conversam
com a IA pelo [Assistente](../Assistente/README.md).

| Ferramenta | Código | Escopo | Como a IA trabalha |
|---|---|---|---|
| [Briefing](Briefings/README.md) | `briefing` | Projeto | Ferramentas locais (`atualizar_briefing`, `gerar_pdf`) |
| [Desenho 2D](Desenho2d/README.md) | `desenho-2d` | Versão | MCP do CADCraft (`cadcraft-cli mcp`) |
| [Modelo 3D](Modelo3d/README.md) | `modelo-3d` | Versão | MCP do SolidCraft (`solidcraft-cli mcp`) |

## Como adicionar uma ferramenta
1. **Domain** (se tiver estado próprio): `Domain/Entities/Ferramentas/<Nome>`.
2. **Application**: `Application/Ferramentas/<Nome>/<Nome>NoAssistente.cs` implementando `IFerramentaComIa`
   (código, se é do projeto, se abre sozinha e o contexto do turno). É registrada sozinha por varredura.
3. **Infra**: `Infra/IA/Ferramentas/<Nome>/<Nome>NaIa.cs` implementando `IFerramentaNaIa` (instruções,
   servidor MCP se houver, texto dos passos). Também registrada por varredura.
4. **Presentation** (só se tiver endpoints além da conversa): `Presentation/WebAPI/Controllers/Ferramentas/<Nome>`.
5. **Front**: `modules/ferramentas/<nome>` usando `<cw-conversa-ia>`; serviços em `data/ferramentas/<nome>`.
6. Um `README.md` na pasta da ferramenta.

Os testes de arquitetura quebram se a nova ferramenta importar outra ou se o Assistente importá-la.

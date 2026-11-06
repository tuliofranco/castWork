# Briefing

O documento que descreve a joia pedida pelo cliente. A IA conversa com o joalheiro, preenche o briefing
e gera o PDF.

- **Código:** `briefing`
- **Escopo:** projeto. Visível em todas as versões só para leitura (ver o PDF); a conversa acontece
  apenas na página do projeto.
- **Abre sozinha:** sim, a IA puxa a conversa ao abrir a ferramenta.
- **Ferramentas da IA (locais, sem MCP):** `atualizar_briefing` e `gerar_pdf` (`FerramentasDoBriefing`).
- **Arquivo de trabalho:** o PDF do briefing.

| Camada | Pasta |
|---|---|
| Domain | `Domain/Entities/Ferramentas/Briefings` (Briefing, DocumentoBriefing, FormularioBriefing…) |
| Application | `Application/Ferramentas/Briefings` |
| Infra | `Infra/IA/Ferramentas/Briefings` (instruções) |
| Presentation | `Presentation/WebAPI/Controllers/Ferramentas/Briefings` |
| Front | `modules/ferramentas/briefing` (rota `projetos/:id/briefing`), `data/ferramentas/briefing` |

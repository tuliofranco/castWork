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
`solidcraft-cli` da mesma versão.

## Instalação

A ponte com o editor usa `document.export`, `document.import` e `document.saved`, que **não existem na release
0.30.1** (a última publicada quando a ferramenta entrou). Até sair uma release com elas, use um build da `main`
do [SolidCraft](https://github.com/tangency-labs/solidcraft). Testado com o commit `73e188d` (0.40.0-rc.2).

1. Pré-requisitos: Rust (`rustup`, com o alvo `wasm32-unknown-unknown`) e [Trunk](https://trunkrs.dev).
2. Build, na pasta do SolidCraft, no commit fixado:
   ```bash
   git checkout 73e188d
   cargo build --release --bin solidcraft-cli
   (cd apps/solidcraft-web && trunk build --release --public-url ./)
   ```
3. Servidor: aponte `SolidCraft:Caminho` (User Secrets ou variável `SolidCraft__Caminho`) para
   `target/release/solidcraft-cli`. Na subida, `VerificacaoSolidCraft` avisa se o caminho não existe.
4. Front: copie o editor web com
   `SOLIDCRAFT_DIST=<pasta-do-solidcraft>/apps/solidcraft-web/dist npm run solidcraft:instalar`.
   `public/solidcraft` fica fora do git; refaça este passo em cada máquina e no build de produção.

CLI e editor web precisam ser do **mesmo commit**: o arquivo JSON (`solidcraft/51`) passa de um para o outro.
Ao atualizar o SolidCraft, troque o commit acima e confira que abrir, salvar e a conversa com a IA seguem funcionando.

# Design

## Visual Theme
Caderno de ateliê: papel de linho quente, tinta escura e um único toque de pátina (o verde-azulado do cobre oxidado). Calmo, organizado, com caráter tipográfico nos títulos. Claro por padrão (luz forte de bancada, apresentação ao cliente); escuro automático pelo sistema, com o mesmo calor.

Cena de referência: joalheiro com o tablet apoiado ao lado da bancada, sob a lâmpada de trabalho, que de vez em quando vira a tela para o cliente no balcão.

## Color Palette
Estratégia **contida**: neutros tingidos para o quente (hue 60–80) e um destaque, pátina (hue 185), usado só em ação principal, seleção e foco. Tokens em `frontend/src/styles/_tokens.scss` (OKLCH).

| Papel | Claro | Escuro |
|---|---|---|
| `--cw-bg` fundo | `oklch(0.972 0.008 80)` linho | `oklch(0.19 0.012 60)` |
| `--cw-surface` superfícies | `oklch(0.993 0.004 80)` papel | `oklch(0.225 0.012 60)` |
| `--cw-surface-2` rail e painéis | `oklch(0.948 0.011 75)` | `oklch(0.165 0.012 60)` |
| `--cw-border` | `oklch(0.885 0.013 75)` | `oklch(0.33 0.013 60)` |
| `--cw-ink` texto | `oklch(0.24 0.015 60)` | `oklch(0.93 0.01 80)` |
| `--cw-ink-muted` | `oklch(0.47 0.016 60)` | `oklch(0.72 0.013 75)` |
| `--cw-accent` pátina | `oklch(0.47 0.085 185)` | `oklch(0.74 0.09 185)` |
| `--cw-accent-soft` | `oklch(0.93 0.03 185)` | `oklch(0.31 0.045 185)` |

Semânticas: sucesso `oklch(0.52 0.12 150)`, aviso `oklch(0.6 0.13 70)`, perigo `oklch(0.52 0.17 28)`, cada uma com fundo suave. Estado nunca só por cor: badge sempre com ícone e rótulo.

## Typography
- **Instrument Sans** (variável, auto-hospedada): toda a interface, rótulos, dados, botões.
- **Fraunces** (variável, opsz alto, peso 560): só a marca e o título de página (`h1`). Nunca em rótulos, botões ou dados.
- Escala fixa em rem, razão ~1.2: 0.8125 / 0.9375 (base) / 1.0625 / 1.25 / 1.625 (h1). Prosa limitada a 68ch.

## Layout
- **Tablet-first** (768–1199 px é o alvo): rail lateral de 88 px com ícone e rótulo; a partir de 1200 px o rail expande para 236 px com rótulo ao lado; abaixo de 768 px vira barra inferior.
- Conteúdo com respiro variável: 28 px de margem lateral no tablet, 40 px no desktop, largura máxima de 1120 px.
- Listas tocáveis em vez de tabelas: cada linha é um alvo inteiro de no mínimo 64 px de altura.
- Detalhe do projeto: composição e biblioteca lado a lado a partir de 1024 px; empilhadas abaixo disso.
- Barra de ações dos formulários fixa no rodapé da área de conteúdo, ao alcance do polegar.

## Components
Componentes de `frontend/src/app/shared/ui/`:
- `cw-cabecalho-pagina`: título (Fraunces), subtítulo, link de voltar, ações.
- `cw-campo-busca`: busca com ícone e botão de limpar.
- `cw-estado-lista`: carregando (esqueleto), erro (com tentar de novo) e vazio (que ensina o próximo passo).
- `cw-paginacao`: total e anterior/próxima por cursor.
- `cw-selo`: badge de status com ícone.
- `cw-campo`: rótulo, dica, "opcional" e mensagens de erro (inclusive do servidor) em volta do controle.
- `cw-quantidade`: stepper − / + com campo, para toque.
- `cw-iniciais`: avatar com iniciais.
- `cw-barra-acoes`: rodapé de formulário com ação principal e cancelar.

Estados obrigatórios em todo interativo: padrão, hover, foco visível (anel de pátina de 3 px), ativo, desabilitado, carregando, erro.

## Elevation & Shape
Raio 10 px (controles e linhas), 14 px (painéis), 999 px (selos e chips). Sombras quase nulas: separação por superfície e borda de 1 px, não por sombra. Sem cards aninhados.

## Motion
150–220 ms, `cubic-bezier(0.22, 1, 0.36, 1)` (ease-out-quint). Só para estado (hover, foco, abertura do menu do usuário, esqueleto). Respeita `prefers-reduced-motion`.

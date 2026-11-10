# Precificação do projeto

Modelo aprovado em 09/11/2026. A precificação leva do modelo 3D ao preço: o joalheiro desenha a peça e já vê
**quanto custa e por quanto vender**. O cálculo é determinístico, no backend (a IA não calcula preço). Roteiro em
etapas na issue #10.

## Preços do ateliê (etapa 1)

O ateliê tem os seus **padrões de preço**. Cada projeto copia esses valores ao ser criado (etapa 3) e pode
ajustá-los ou "atualizar para os valores atuais".

### Cotações

- Sempre do **metal puro**, em R$/g: ouro 24k e prata fina. O sistema nunca pede "R$/g do 18k": a pureza da liga
  entra no cálculo pelo teor (`TeorMetal.Pureza`: 0,750 no 18k).
- Metais: **Ouro** e **Prata**. Platina e outros entram depois como novo `Metal`.
- Cada atualização é uma linha nova (`metal`, `valor`, `registrada_em`, `registrada_por`); nada é sobrescrito.
  A vigente é a mais recente.
- A tela mostra a idade da cotação ("atualizada há N dias") e avisa quando passa de **15 dias**.
- Sem cotação automática de mercado nesta etapa: o ateliê informa o valor.

### Ligas

Nome, metal base, teor e **densidade** (g/cm³). Vêm pré-cadastradas e são editáveis; liga em uso não é excluída,
é desativada (como a peça da biblioteca).

| Liga | Teor | Densidade (g/cm³) |
|---|---|---|
| Ouro 24k | 999 | 19,3 |
| Ouro 18k amarelo | 750 | 15,5 |
| Ouro 18k branco | 750 | 15,0 |
| Ouro 18k rosé | 750 | 15,1 |
| Ouro 14k amarelo | 585 | 13,4 |
| Prata 950 | 950 | 10,4 |
| Prata 925 | 925 | 10,36 |

O teor dá a fração de metal puro (preço); a densidade converte o volume do modelo 3D em peso (etapa 6).

### Parâmetros

| Parâmetro | O que é | Padrão | Faixa |
|---|---|---|---|
| Perda de fundição e acabamento | % de metal que se perde entre o 3D e a peça pronta (canais, botão, polimento) | 8% | 0% a 50% |
| Valor da hora | Hora de bancada (R$) | a definir | maior que zero |
| Markup | Fator sobre o custo (1,8 = custo × 1,8) | 1,8× | maior que zero, até 10× |
| Taxas | % de cartão e impostos, somada sobre o preço | 0% | 0% a 50% |
| Arredondamento | O preço sobe até o múltiplo deste valor | R$ 10 | R$ 0 (não arredonda) a R$ 1.000 |

- O valor da hora já existia nos dados do ateliê: passa a ser parâmetro de preço, alterado na tela de preços.
  Instalação nova começa com R$ 0,00 ("a definir"); a tela pede o valor e só salva com valor positivo.
- Os padrões são o default das colunas no banco: valem para a linha que já existe.

## Custo das peças (etapa 2)

As peças da biblioteca (pedras, fechos, correntes compradas prontas) têm **custo por unidade** (R$), opcional. É o
custo de **compra ou produção**, não o preço de venda; peça sem custo deixa o preço da versão incompleto (etapa 4) e
aparece com o selo "Sem custo" na lista. Pedra não é precificada pelo volume do 3D: o custo dela vem daqui. Só quem tem
`precificacao.ler` vê (e grava) o custo. Custo por quilate ou tamanho e histórico de custos ficam para depois.

## Preços do projeto (etapa 3)

Cada projeto tem **os próprios preços**: ao ser criado, copia as cotações vigentes (valor e data de origem de cada metal
já cotado) e os parâmetros do ateliê, com a data "herdado em". Assim o orçamento de um cliente não muda sozinho quando o
ateliê atualiza o preço do ouro.

- **Editar** muda só aquele projeto (nunca o ateliê nem os outros projetos) e marca "editado neste projeto em". Cotação
  que mantém o valor mantém a data de origem; metal deixado em branco fica sem cotação.
- **Atualizar para os valores atuais** substitui tudo (cotações e parâmetros) pelos de hoje do ateliê, com confirmação.
- A página do projeto mostra a diferença para o ateliê de hoje ("ateliê hoje: Ouro R$ 655,00/g").
- Projeto com o valor da hora "a definir" herda assim; ao editar, a tela pede o valor.
- Projetos que já existiam receberam os preços atuais do ateliê na migration, com "herdado em" = data da migration.
- Nada é congelado aqui: a proposta (item 2 do roadmap) é que congela o preço calculado.
- Ver os valores exige `precificacao.ler` (sem a regra, o bloco nem aparece); editar e atualizar, `precificacao.editar`.

## Preço da versão (etapa 4)

Cada versão tem o **preço calculado**, linha a linha. Nesta etapa o peso do metal é **digitado** (estimado); as etapas 5
a 7 trocam pelo peso medido no modelo 3D.

- **Metais da versão:** liga + peso estimado da peça pronta (g). Pode haver mais de uma liga (ouro amarelo e branco).
- **Mão de obra:** por horas (horas × valor da hora do projeto) ou valor fixo.
- **Serviços externos:** lista livre de nome + valor (fundição, cravação, ródio, gravação).
- **Markup da versão:** opcional; sem ele, vale o do projeto.
- **Peso real** (por liga): preenchido depois de pesar a peça pronta. Não muda o preço; a tela mostra
  "estimado 5,00 g · real 5,20 g · +4%".

| Linha | Exemplo |
|---|---|
| Por liga: peso + perda = peso a fundir × cotação do puro × pureza | Ouro 18k amarelo: 5,00 g + 8% = 5,40 g × R$ 480,00/g (R$ 640,00 × 0,750) = R$ 2.592,00 |
| Composição: Σ quantidade × custo da peça | R$ 800,00 |
| Mão de obra | 6 h × R$ 90,00 = R$ 540,00 |
| Serviços | fundição R$ 120 + ródio R$ 60 = R$ 180,00 |
| **Custo total** | **R$ 4.112,00** |
| **Preço** = custo × markup × (1 + taxas), arredondado para cima | 4.112 × 1,8 × 1,06 = 7.845,70 → **R$ 7.850,00** |

- **Pendências** (preço **Incompleto**, mas o parcial aparece): peça da composição sem custo, metal sem cotação no
  projeto, versão sem nenhum metal com peso, liga desativada em uso e mão de obra por horas com o valor da hora do
  projeto "a definir". A tela tem um atalho para cada uma (abrir a peça, preços do projeto, trocar a liga).
- **Avisos** (não bloqueiam): cotação de um metal usado com mais de 15 dias.
- O cálculo é **sempre refeito na leitura**: nada é gravado. Congelar o preço é papel da proposta.
- A IA **não** calcula nem altera preço.
- Versão publicada ou arquivada não aceita alterar o orçamento; só registrar o peso real. Duplicar copia o orçamento
  (sem o peso real).
- Sem `precificacao.ler`, a versão mostra **só o preço final e o status**; montar o orçamento exige `precificacao.editar`.

## Peso medido no modelo 3D (etapa 6)

O peso do metal da versão passa a vir **do modelo 3D**: o sistema mede o arquivo atual no SolidCraft (sem IA), soma a
massa de cada corpo por liga e usa no cálculo da etapa 4.

- **Material obrigatório:** com qualquer corpo sem material (inclui o "Default" do SolidCraft), o preço fica
  **Incompleto** com a pendência "N corpos sem material", com atalho para o Modelo 3D (a conferência é a etapa 7).
- **Fora do peso:** corpos "Pedra" (o custo da pedra vem da composição) e "Apoio (não conta)".
- **Versão com 3D e peso manual:** vale o 3D; o manual fica guardado, não é usado, e a tela diz isso. Versão **sem 3D**
  continua com o peso manual, marcado como "estimado manualmente".
- **Avisos** (não bloqueiam): corpos de metal sobrepostos (a parte comum seria contada duas vezes) e corpo aberto
  (volume pouco confiável; fica fora do peso).
- **Medição guardada** por arquivo (id) e materiais: só mede de novo quando o arquivo muda (ou a densidade de uma liga).
- **Falha** do SolidCraft não derruba a tela: o preço aparece Incompleto com a pendência "não foi possível medir o
  modelo".
- Sem migration: a medição não é gravada no banco.

## Regras

- Valores negativos ou zero são recusados: cotação, densidade, valor da hora e markup.
- Toda mudança de cotação fica no histórico com data e autor.
- Consultar os preços exige `precificacao.ler`; alterar, `precificacao.editar`. Sem editar, a tela é só leitura;
  sem consultar, ela não aparece.

## Próximas etapas (issue #10)

1. Preços do ateliê.
2. Custo unitário nas peças da biblioteca.
3. Projeto herda e edita os preços do ateliê.
4. Preço da versão com peso manual.
5. Materiais do ateliê no arquivo 3D e IA atribuindo material.
6. Peso do metal medido no modelo 3D (material obrigatório para o preço).
7. Conferência de peso e materiais da versão.

## Fora desta etapa

Conferência de peso e materiais (etapa 7), proposta, PDF e congelamento do preço, atualizar vários projetos de uma vez, cotação
automática de mercado, platina e outros metais.

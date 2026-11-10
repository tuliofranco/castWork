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

Usar os valores em projetos e versões, cotação automática de mercado, platina e outros metais.

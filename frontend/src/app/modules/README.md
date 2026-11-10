# modules/

Telas por domínio, **sempre com lazy loading** (`loadChildren` / `loadComponent`).

`clientes/`, `pecas/`, `precificacao/`, `documentos/`, `portfolio/`

- `pecas/`: biblioteca (lista, `nova` e `:id/editar`). Custo por unidade e selo "Sem custo" só com `precificacao.ler`; edição com `pecas.editar`.
- `precificacao/`: preços do ateliê (cotações com histórico, ligas e parâmetros). Sem `precificacao.editar`, só leitura.
- `projetos/`: na página do projeto, o bloco **Preços deste projeto** (`cw-precos-projeto`): herdado em, editar, atualizar para os valores atuais (com confirmação) e a diferença para o ateliê de hoje. Só aparece com `precificacao.ler`; editar e atualizar pedem `precificacao.editar`.
- `projetos/`: na página da versão, o card **Preço** (`cw-preco-versao`): o cálculo linha a linha com o peso do metal **medido no modelo 3D** ("Peso da peça (3D) · medido no modelo") ou, sem 3D, **estimado manualmente**, o preço em destaque, o selo Completo/Incompleto com as pendências e um atalho para cada uma, avisos (cotação antiga, corpos de metal sobrepostos, corpo aberto) e a edição de metais e peso, mão de obra (horas ou valor fixo), serviços, markup e peso real ("estimado 5,00 g · real 5,20 g · +4%"). Sem `precificacao.ler`, só o preço e o status; editar pede `precificacao.editar` e versão em desenvolvimento (o peso real vale em qualquer status). Com 3D: a pendência "N corpos sem material" e o aviso de corpos sobrepostos têm o atalho **Conferir**, que abre a conferência de peso e materiais por cima do Modelo 3D (`modelo-3d?conferir=1`); "não foi possível medir" leva ao Modelo 3D; o peso manual fica guardado e o card diz que não é usado ("Peso manual guardado e não usado (vale o 3D)"); "Metais e peso" edita esse manual; o peso real só se registra nas ligas que têm peso manual.

- `ferramentas/modelo-3d/`: aviso discreto "N corpos sem material" por cima do SolidCraft (sem bloquear), conferido pelo `inspect.measure` do SolidCraft embutido ao abrir, salvar e trazer o que a IA fez. O "Default" do SolidCraft conta como sem material (`corpos-sem-material.ts`).
- `ferramentas/modelo-3d/`: **Peso e materiais** (`cw-conferencia-modelo`), aberta pelo botão do Modelo 3D, pelo "Conferir" do aviso de corpos sem material ou por `?conferir=1` (atalho do card Preço). Lista os corpos com material, volume e peso (peso do metal no topo), destaca os sem material e deixa escolher o material de um ou vários corpos e **Aplicar**; une corpos de metal da mesma liga que se sobrepõem (**Unir corpos**, com confirmação). Cada alteração vira um arquivo novo com **Desfazer** (restaura o de antes) e o SolidCraft à vista recarrega. Antes de abrir, o que foi modelado à mão é salvo. Alterar pede `projetos.editar` e versão em desenvolvimento; senão, só leitura. A tela não cria nem apaga corpos.

**Regra central:** um módulo nunca importa outro (o ESLint bloqueia imports de `@modules/*`). O que precisar ser compartilhado sobe para `shared/` ou `data/`.

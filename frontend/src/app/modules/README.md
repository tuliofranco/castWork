# modules/

Telas por domínio, **sempre com lazy loading** (`loadChildren` / `loadComponent`).

`clientes/`, `pecas/`, `precificacao/`, `documentos/`, `portfolio/`

- `pecas/`: biblioteca (lista, `nova` e `:id/editar`). Custo por unidade e selo "Sem custo" só com `precificacao.ler`; edição com `pecas.editar`.
- `precificacao/`: preços do ateliê (cotações com histórico, ligas e parâmetros). Sem `precificacao.editar`, só leitura.
- `projetos/`: na página do projeto, o bloco **Preços deste projeto** (`cw-precos-projeto`): herdado em, editar, atualizar para os valores atuais (com confirmação) e a diferença para o ateliê de hoje. Só aparece com `precificacao.ler`; editar e atualizar pedem `precificacao.editar`.
- `projetos/`: na página da versão, o card **Preço** (`cw-preco-versao`): o cálculo linha a linha com o peso **estimado**, o preço em destaque, o selo Completo/Incompleto com as pendências e um atalho para cada uma, avisos (cotação antiga) e a edição de metais e peso, mão de obra (horas ou valor fixo), serviços, markup e peso real ("estimado 5,00 g · real 5,20 g · +4%"). Sem `precificacao.ler`, só o preço e o status; editar pede `precificacao.editar` e versão em desenvolvimento (o peso real vale em qualquer status).

- `ferramentas/modelo-3d/`: aviso discreto "N corpos sem material" por cima do SolidCraft (sem bloquear), conferido pelo `inspect.measure` do SolidCraft embutido ao abrir, salvar e trazer o que a IA fez. O "Default" do SolidCraft conta como sem material (`corpos-sem-material.ts`).

**Regra central:** um módulo nunca importa outro (o ESLint bloqueia imports de `@modules/*`). O que precisar ser compartilhado sobe para `shared/` ou `data/`.

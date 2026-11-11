# Code review do frontend do Castwork

Branch `chore/code-review-frontend`. Escopo: `frontend/src/app` (Angular 21, Apollo/GraphQL, Bootstrap), com o backend usado só para conferir contratos. Nenhum código do projeto foi alterado; este relatório é o único arquivo novo.

Caminhos relativos à raiz do repositório. Esforço: P (pequeno), M (médio), G (grande).

## 1. Resumo executivo

**Saúde geral.** O frontend está bem no caminho feliz e frágil nos caminhos de erro, de corrida e de ciclo de vida. Lint, tipos, 264 testes e build passam; a camada `data` não guarda estado e bate campo a campo com o schema GraphQL e com os controllers REST; não há `innerHTML`, `bypassSecurityTrust*`, `postMessage` nem token em URL. Os problemas se concentram em quatro frentes:

1. **Orquestração assíncrona das ferramentas (Desenho 2D e Modelo 3D).** Não há exclusão mútua entre abrir, trocar arquivo, salvar, restaurar e marcar final; o sinal `trocando` não entra em nenhuma guarda; não há proteção ao sair com trabalho não salvo. O código foi copiado entre as duas ferramentas e já divergiu.
2. **Caminhos de erro.** No Angular 21.2.26 instalado, `resource.value()` lança quando o recurso está em erro, e o padrão `@let x = recurso.value()` está em quase todas as telas: as mensagens com "Tentar de novo" não chegam a renderizar. Erros de download (`responseType: 'blob'`) e promessas sem `catch` somem em silêncio.
3. **Política de sessão.** Qualquer falha do refresh desloga e revoga a sessão no servidor, e o `authGuard` descarta o refresh token de 7 dias quando o access token de 15 minutos vence.
4. **Testes.** A suíte quase não exercita falha, e os componentes de maior risco (`Modelo3dComponent`, `Desenho2dComponent`, as duas pontes, `ConversaIaComponent`, o parser de SSE) não têm spec.

**Contagem.** As 8 áreas e as ferramentas produziram 125 achados; depois de juntar duplicatas ficaram **92: 0 Críticos, 6 Altos, 46 Médios, 40 Baixos**. Todas as áreas previstas foram revisadas. Dezesseis achados passaram por verificação independente (os que chegaram como Alto): 15 CONFIRMADOS (quase todos com teste temporário que reproduziu o defeito), 1 PLAUSÍVEL e nenhum derrubado; nove foram rebaixados para Médio pelo verificador. Consolidados, eles correspondem a 11 achados deste relatório (A1 a A6 e M1 a M5). Médios e Baixos de origem não passaram por verificação.

**Os 5 problemas mais graves**

1. **A3 — Sair do Modelo 3D (ou do Desenho 2D) com modelagem não salva perde o trabalho sem aviso.** Não existe `canDeactivate`, `beforeunload` nem autosave; o link "‹ Versão" destrói o componente e os iframes sem consultar a ponte.
2. **A2 e M1 — A troca de arquivo por trás (dois iframes) não é serializada nem bloqueia as ações.** Duas trocas simultâneas deixam os dois iframes em `about:blank` (tela morta até o F5); Salvar, ⌘S ou enviar mensagem durante "Atualizando o modelo…" grava o arquivo antigo por cima do que a IA fez, e tela e servidor divergem em silêncio.
3. **A1 — `resource.value()` lido sem guarda lança em estado de erro.** São cerca de 30 pontos em projetos, peças, precificação, clientes, briefing e nas duas ferramentas: a lista fica presa no esqueleto, a tela de erro é código morto e a falha de um recurso secundário (briefing, pesos, clientes) derruba a página inteira.
4. **A6 (com M4) — A sessão cai quando deveria renovar.** Uma queda de rede, um 429 ou um 5xx no refresh viram logout com revogação da família de tokens no servidor e redirecionamento para o login, destruindo a tela atual; o `authGuard` joga fora um refresh token válido.
5. **A5 — "Duplicar versão" termina sempre em "Versão não encontrada".** O projeto só é relido quando muda o `:id`; a cópia existe, mas a tela mostra um erro falso logo depois do toast de sucesso, até o F5. O A4 (Restaurar no Desenho 2D deixa a tela presa em "Guardando o seu desenho...") é da mesma gravidade e se corrige com uma linha.

**Sobre o sintoma "Abrindo o modelo da versão…".** Três áreas procuraram a causa e um teste com as rotas reais mostrou que o router reutiliza a mesma instância do `Modelo3dComponent` na mesma rota; nada no app o recria. O texto só reaparece com instância nova: recarga do documento (rebuild do `ng serve` em mudança de `.ts`, aba descartada por memória) ou ida e volta de rota (logout por refresh falho, A6/M4). A causa exata não foi provada pela leitura e fica como suspeita com roteiro de instrumentação (M2).

**Saída das ferramentas**

| Comando | Passou | Resumo |
|---|---|---|
| `npm run lint` | Sim | Exit 0. "All files pass linting." 0 erros, 0 avisos. |
| `npx knip` | Sim | Exit 0, saída vazia. Rodou com knip 6.45.1 baixado pelo npx: o pacote não está em `node_modules` nem em `devDependencies`. O `knip.json` desliga a regra `types` e ignora bootstrap, bootstrap-icons e prettier. |
| `npx tsc --noEmit -p tsconfig.app.json` | Sim | Exit 0, saída vazia. 0 erros de tipo. |
| `npm run test:ci` | Sim | Exit 0 (Vitest 4.1.12). 56 arquivos e 264 testes passaram, 0 falhas, 3,98 s. |
| `npm run build` | Sim | Exit 0, 0 erros, 1 aviso de budget: `ferramenta-ia.component.scss` com 7,94 kB contra erro em 8 kB. Initial total 726,00 kB (aviso em 750 kB). Maiores lazy chunks: heic-to 3,23 MB, pdf 432,87 kB. |

**Limites desta revisão.** Os `.scss` não foram revisados. O comportamento interno do SolidCraft e do CADCraft (WebAssembly em `frontend/public`) não foi exercitado: nenhuma reprodução foi feita no navegador com eles, só em testes de componente com as pontes simuladas. `DESIGN.md` e `PRODUCT.md` não foram lidos pela área de core.

## 2. Achados por severidade

Convenções desta seção:

- **Veredito.** Só os achados que chegaram como Alto passaram por verificação independente; o veredito deles aparece no próprio achado (CONFIRMADO ou PLAUSÍVEL). **Todos os demais Médios e Baixos são NÃO VERIFICADOS**: apoiam-se na leitura do revisor da área. Isso vale para todo achado sem a linha "Veredito".
- **Suspeita.** O que não deu para provar lendo o código está marcado como **Suspeita**, com a forma de confirmar.
- **Fusão.** Quando duas áreas relataram o mesmo defeito, ficou um achado só, com todos os locais. Na fusão vale a severidade ajustada pelo verificador; sem verificador, a maior das duas, salvo indicação.
- Não há achado Crítico.

### Alto

#### A1 — `resource.value()` lido sem guarda lança em estado de erro: telas de erro inalcançáveis e páginas derrubadas por recurso secundário

- **Onde (confirmados por teste):** `frontend/src/app/modules/projetos/projetos.component.html:11`; `frontend/src/app/modules/projetos/projeto-detalhe/projeto-detalhe.component.html:13` e `:28` (briefing); `frontend/src/app/modules/pecas/pecas.component.html:7`; `frontend/src/app/modules/ferramentas/briefing/briefing.component.html:1` e `briefing.component.ts:80`, `:92`.
- **Onde (mesmo padrão, conferido só por leitura):** `modules/clientes/clientes.component.html:11`; `modules/precificacao/precificacao.component.html:14`; `modules/projetos/projeto-configuracoes/projeto-configuracoes.component.html:38`, `:68`, `:96`; `modules/projetos/novo-projeto/novo-projeto.component.html:12`; `modules/projetos/versao-detalhe/versao-detalhe.component.html:78`, `:89` e `versao-detalhe.component.ts:107` (`pesoTotal`); `modules/projetos/peca-do-projeto/peca-do-projeto.component.html:1`, `:48` e `.ts:166`, `:194`, `:303`, `:310`; `modules/projetos/projeto-detalhe/pecas-projeto.component.ts:44`; `precos-projeto.component.ts:54`, `:252`; `modules/projetos/versao-detalhe/preco-versao.component.html:4`, `:241`; `modules/ferramentas/modelo-3d/modelo-3d.component.ts:72-80` e `conferencia-modelo.component.ts:83-96` (html 4, 15, 122); `modules/ferramentas/desenho-2d/desenho-2d.component.ts:64` (todos sob `frontend/src/app`).
- **Trecho:**
  ```html
  @let pagina = projetos.value();
  <cw-estado-lista [carregando]="!projetos.hasValue() && projetos.isLoading()" [erro]="!!projetos.error()" ...>
  ```
  ```js
  // node_modules/@angular/core/fesm2022/_resource-chunk.mjs:176-177 (21.2.26)
  if (!isResolved(streamValue)) { throw new ResourceValueError(this.error()); }
  ```
- **Problema:** `value()` de um resource em erro lança `ResourceValueError` (só devolve o padrão enquanto está `loading`; `defaultValue` não muda isso). A leitura vem antes do ramo de erro, então os bindings seguintes não são atualizados. O `erroGlobalInterceptor` mostra o toast e relança, e nenhum stream tem `catchError`.
- **Cenário:** (a) API fora ao abrir `/projetos`, `/pecas` ou `/clientes`: o esqueleto de carregamento fica para sempre, sem "Não foi possível carregar…" nem "Tentar de novo"; só o toast. Falha ao paginar ou buscar congela a lista anterior. (b) Página do projeto com o projeto carregado e o GET do briefing em 500: se o briefing falha antes de o projeto chegar, a página fica meio montada (sem título, preços e versões); se falha depois, a view congela e lança a cada detecção de mudanças. (c) Briefing: `getProjeto`, usado só no subtítulo, derruba a tela inteira; o `briefing.reload()` de `aposResposta()` que falha congela a conversa. (d) Conferência do Modelo 3D: medição falha e o diálogo modal fica em branco.
- **Correção:** nunca ler `value()` sem guarda. Criar um helper em shared, por exemplo `valorDe<T>(r: Resource<T>): T | undefined => (r.hasValue() ? r.value() : undefined)`, e trocar as leituras. Atenção: `hasValue()` é falso para `undefined` e verdadeiro para `null`, então `valorDe(briefing) === null` continua significando "sem briefing (204)". Nos templates, `@let pagina = x.hasValue() ? x.value() : undefined;` ou ordenar `@if (x.error()) … @else if (…)`. Para recursos secundários preferir o helper a `catchError(() => of(null))` quando `null` tem significado de domínio (briefing); onde não tem (clientes, ligas, finais), `catchError(() => of([]))` serve. No briefing, trocar `briefing.reload()` de `aposResposta()` por `getBriefing(...)` + `briefing.set(b)`, que mantém o valor anterior se falhar; reordenar o `@if` da linha 2 para testar `briefing.error()` primeiro. Na conferência, desenhar o ramo "Não foi possível medir. Tentar de novo". Acrescentar um spec de erro por tela (`throwError` no serviço, esperando `[role=alert]` e o botão). Vale uma regra de lint ou de revisão para o padrão.
- **Esforço:** M (P por tela).
- **Veredito:** CONFIRMADO em projetos, projeto-detalhe, peças e briefing (três verificadores, testes de componente com `ErrorHandler` coletor). Os demais locais seguem o mesmo mecanismo e não foram testados um a um. O verificador do briefing classificou aquele local isolado como Médio; os de projetos e peças mantiveram Alto.

#### A2 — Duas trocas de arquivo simultâneas deixam os dois iframes em `about:blank` (Modelo 3D e Desenho 2D)

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:294-313`; `frontend/src/app/modules/ferramentas/desenho-2d/desenho-2d.component.ts:210-228` (cópia).
- **Trecho:**
  ```ts
  const atras = this.ativo() === 0 ? 1 : 0;
  const nova = this.criarPonte(atras);
  ...
  await nova.carregar(...);
  const antiga = this.ponte;
  this.ponte = nova;
  this.ativo.set(atras);
  antiga?.descarregar();
  ```
- **Problema:** `trazerArquivo()` não tem trava nem fila. `atras` vem de `ativo()`, que só muda depois do `await nova.carregar(...)` (segundos de WebAssembly). Duas chamadas nessa janela criam duas pontes sobre o mesmo iframe; a segunda encontra como `antiga` a ponte do iframe que acabou de virar o ativo e o descarrega. Se uma falhar depois de a outra dar certo, o `catch` apaga o mesmo iframe. `this.ponte` fica apontando para um iframe vazio: todo salvar falha.
- **Cenário:** (a) Histórico, duplo clique em "Restaurar" sem alterações à mão: `restaurar()` não tem guarda e só liga `sincronizando` depois do POST; saem dois `postRestaurar` e duas trocas. (b) "Desfazer" em dois cartões do chat em sequência, ou no cartão recém-criado enquanto aparece "Atualizando…": `conversa-ia.component.ts:177-180` não tem guarda e o botão em `shared/ui/ferramenta-ia/ferramenta-ia.component.ts:194` não tem `[disabled]`. (c) A IA termina e, durante a troca, o joalheiro restaura, marca final ou monta: nenhuma guarda olha `trocando()`. Resultado: prancheta vazia até o F5. Nada se perde no servidor.
- **Correção:** serializar `trazerArquivo()` nos dois componentes: guardar a Promise em andamento e um sinalizador de pedido pendente; se chegar outro pedido durante a troca, devolver a mesma Promise e, ao terminar, rodar mais uma vez buscando o arquivo atual. O `getArquivo` e o cálculo de `atras` ficam dentro da seção serializada. Mover `atualId.set(...)` para depois de o `carregar` dar certo. Em `restaurar()`, checar `sincronizando() || pensando() || trocando()` na entrada, ligar `sincronizando` antes do primeiro `await` e limpar em `finally` (no 2D, ver A4 para o ponto certo de limpar). Em `conversa-ia.component.ts:177`, ignorar um Desfazer enquanto outro está em andamento. "Só descarregar se o iframe da antiga for diferente do da nova" serve como defesa extra, não como correção (deixaria dois `document.import` concorrentes). Ao extrair a base comum (M15), o fluxo passa a existir uma vez.
- **Esforço:** M.
- **Veredito:** CONFIRMADO nas duas ferramentas, com teste do componente real e pontes simuladas (os dois iframes terminam com `src="about:blank"`). Não reproduzido no navegador com SolidCraft/CADCraft reais. O verificador do 3D manteve Alto; o do 2D classificou a cópia como Médio (janela de segundos, sem perda de dados).

#### A3 — Sair da ferramenta com trabalho não salvo perde tudo sem aviso

- **Onde:** `frontend/src/app/app.routes.ts:27-32` e `:41-46` (Modelo 3D); `:21-26` e `:35-40` (Desenho 2D, mesma lacuna).
- **Trecho:**
  ```ts
  path: 'projetos/:id/versoes/:versaoId/modelo-3d',
  canActivate: [authGuard, permissionGuard],
  data: { title: 'Modelo 3D', permissoes: ['projetos.ler'] },
  ```
- **Problema:** não existe `canDeactivate` em todo o app, nem `beforeunload`, `pagehide` ou autosave. O que foi modelado à mão só vai ao Castwork em Salvar/⌘S ou antes de uma mensagem à IA, conferência, montagem, restauração ou marcação de final. O "voltar" é um `<a [routerLink]>` puro (`shared/ui/ferramenta-ia/ferramenta-ia.component.ts:68`); `ponte.temAlteracoes()` não é consultado em nenhum caminho de saída. Ao reabrir, `iniciar()` sempre importa o arquivo do servidor.
- **Cenário:** o joalheiro modela 30 minutos à mão, sem falar com a IA, e toca em "‹ Versão" (ou usa o Voltar dentro do app, ou a sessão expira e o interceptor navega para `/login`): o trabalho some sem pergunta.
- **Correção:** (1) `canDeactivate` funcional nas rotas das duas ferramentas chamando um método público do componente (`podeSair(): Promise<boolean>`) que consulta `ponte.temAlteracoes()` com try/catch e timeout curto (em erro ou timeout, deixar sair) e oferece Salvar e sair / Descartar / Continuar. (2) O guard libera sem perguntar quando o destino é `/login` por sessão expirada (o salvar falharia com 401). (3) `beforeunload` precisa ser síncrono: manter um signal `sujo` atualizado por evento ou sondagem leve e registrar `window:beforeunload` enquanto for verdadeiro; antes, confirmar se o SolidCraft em `?embed=editor` já registra o seu. (4) Autosave é opcional e tem custo de produto (cada `putArquivo` vira versão no histórico): se adotado, usar rascunho separado e não disparar com `sincronizando()` ou `pensando()`.
- **Esforço:** M (G com autosave/rascunho).
- **Veredito:** CONFIRMADO para a navegação dentro do SPA (teste com `RouterTestingHarness`: sai sem consultar `temAlteracoes()`). **Suspeita** nas partes "fechar a aba sem pergunta" e "não há rascunho para recuperar": o bundle do SolidCraft 0.40.0-rc.2 tem strings de autosave e `onbeforeunload`, e a única evidência de que o modo embed desliga isso é o comentário em `ponte-solidcraft.ts:19-20`. Como confirmar: abrir `/solidcraft/?embed=editor`, criar um corpo e (a) fechar a aba para ver se o navegador pergunta; (b) reabrir e ver se há oferta de recuperação.

#### A4 — Restaurar no Desenho 2D deixa a tela presa em "Guardando o seu desenho..."

- **Onde:** `frontend/src/app/modules/ferramentas/desenho-2d/desenho-2d.component.ts:141-157`.
- **Trecho:**
  ```ts
  if (this.ponte?.temAlteracoes()) {
    this.sincronizando.set('Guardando o seu desenho...');
    await this.guardar(await this.ponte.salvar());
  }
  ...
  await this.trazerArquivo();
  this.toast.sucesso('Desenho restaurado.');
  } catch (erro) { ... this.sincronizando.set(null); }
  ```
- **Problema:** `sincronizando` só volta a `null` dentro do `catch`. No caminho de sucesso com alterações à mão ele fica com a mensagem para sempre; `trazerArquivo()` e `guardar()` não mexem nesse signal. O gêmeo do 3D (`modelo-3d.component.ts:228`) tem o `set(null)` antes de `trazerArquivo`.
- **Cenário:** desenhar uma linha (título do CADCraft com "•"), abrir o Histórico e restaurar uma versão antiga. A restauração funciona, mas o overlay cobre o CADCraft e Salvar, Histórico e Marcar como final ficam desabilitados até recarregar a página. Não há perda de dados.
- **Correção:** acrescentar `this.sincronizando.set(null);` logo antes de `await this.trazerArquivo();`, mantendo o do `catch`. Não usar `finally`: o overlay escuro ficaria sobre o CADCraft durante toda a troca, que a tela desenha como aviso discreto. Opcional: dar a `restaurar()` a guarda de entrada de `marcarComoFinal`.
- **Esforço:** P.
- **Veredito:** CONFIRMADO (teste: após `restaurar()` com sucesso, `sincronizando()` continua `'Guardando o seu desenho...'`; sem alterações à mão termina `null`).

#### A5 — Duplicar versão abre "Versão não encontrada": o projeto não é relido quando só o `:versaoId` muda

- **Onde:** `frontend/src/app/modules/projetos/versao-detalhe/versao-detalhe.component.ts:85-92` e `:193-204`; template `versao-detalhe.component.html:3`.
- **Trecho:**
  ```ts
  protected readonly projeto = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.projetosService.getProjeto(params),
  });
  protected readonly versao = computed(() => this.projeto.value()?.versoes.find((v) => v.id === Number(this.versaoId())));
  ```
- **Problema:** `duplicar()` navega para `/projetos/:id/versoes/<novoId>`, a mesma configuração de rota. O Angular reaproveita o componente e só troca o input `versaoId`. Como `params` depende só de `id`, nada é relido: `versao()` vira `undefined` e o template cai em `projeto.hasValue() && !versao()`. `getProjeto` é `apollo.query` de uma vez só e o POST é REST, então nada reemite.
- **Cenário:** versão 20 aberta, clicar em Duplicar: toast "Nova versão criada…", URL `/projetos/1/versoes/30` e a tela mostra "Versão não encontrada · Ela pode ter sido removida…" até o F5. Acontece em 100% das vezes. Efeito secundário (Baixo): `publicacaoBloqueada`, `quantidadesNovas` e a busca do pai sobrevivem entre versões.
- **Correção:** incluir a versão no `params`: `params: () => ({ id: Number(this.id()), versaoId: Number(this.versaoId()) })` com `stream: ({ params }) => this.projetosService.getProjeto(params.id)`. A troca põe o recurso em `loading`, o template cai no esqueleto e os filhos (`cw-preco-versao`, etapas, arquivos) são recriados. Não usar só `projeto.reload()` junto do `navigate`: o reload mantém o valor antigo e "Versão não encontrada" ainda pisca. Zerar o estado local do pai quando `versaoId` muda (`linkedSignal`). Trocar o spec "deve duplicar e abrir a cópia" (`versao-detalhe.component.spec.ts:175`, que só confere `Router.url`) por um com `RouterTestingHarness` conferindo o h1 da cópia e duas chamadas a `getProjeto`.
- **Esforço:** P.
- **Veredito:** CONFIRMADO (teste: h1 "Versão não encontrada", `getProjeto` chamado 1 vez, mesma instância do componente).

#### A6 — Qualquer falha do refresh (rede, 429, 5xx) desloga e revoga a sessão no servidor

- **Onde:** `frontend/src/app/core/interceptors/jwt.interceptor.ts:49-53`; `frontend/src/app/core/services/auth.service.ts:77-82`.
- **Trecho:**
  ```ts
  return auth.renovarToken().pipe(
    catchError(() => {
      auth.logout(router.url);
      return throwError(() => erroOriginal);
    }),
  ```
- **Problema:** o `catchError` ignora o tipo do erro. Status 0, 429 e 5xx são tratados como sessão inválida. `logout()` chama `auth/logout`, que revoga a família inteira do refresh token (`LogoutCommand`), limpa o storage e navega para `/login`, destruindo a tela atual (não há `canDeactivate`, ver A3). O backend limita login e refresh a 10 por minuto por IP; não foi encontrado `UseForwardedHeaders` em `backend/src`.
- **Cenário:** Modelo 3D com o access token vencido, tablet com Wi-Fi instável. "Salvar": o PUT volta 401, o POST `auth/refresh` falha com status 0, 429 ou 503; a sessão é revogada e a tela vai para o login. O modelo feito à mão se perde (inferência da ausência de guard e autosave, não exercitada).
- **Correção:** receber o erro no `catchError` e só chamar `auth.logout(router.url)` quando ele não for `HttpErrorResponse` (caso "sem refresh token") ou quando o status for 400, 401 ou 403. Para 0, 429 e 5xx, manter os tokens e propagar o erro do refresh (não o 401 original), para o `erroGlobalInterceptor` mostrar "sem conexão / tente novamente"; no 429, respeitar o `Retry-After`. Não fazer retry cego em status 0: o refresh token é rotativo e o `RenovarSessaoCommand` revoga a família no reuso; retry automático só é seguro para 429 e 503. Atualizar o teste "se o refresh falhar, deve deslogar" em `jwt.interceptor.spec.ts`. Conferir `UseForwardedHeaders` no deploy.
- **Esforço:** P.
- **Veredito:** CONFIRMADO (teste com interceptor e serviços reais: nos três status sai POST `auth/logout`, o storage é limpo e há navegação para `/login`). **Suspeita** sobre a cota de 429 ser dividida por todos os clientes atrás de um proxy: confirma-se na configuração de deploy.

### Médio

#### M1 — Salvar, ⌘S ou enviar mensagem durante "Atualizando…" grava o arquivo antigo por cima do que a IA fez (3D e 2D)

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:200-201` e `modelo-3d.component.html:32`; `frontend/src/app/modules/ferramentas/desenho-2d/desenho-2d.component.html:14,18,21` e `desenho-2d.component.ts:100`, `:124`, `:233`.
- **Trecho:** `if (!this.ponte || this.sincronizando() || this.pensando()) return;` e `[disabled]="!!sincronizando() || pensando()"`.
- **Problema:** durante `trazerArquivo`, `this.ponte` ainda é o editor antigo, `sincronizando` é `null` e `pensando` já voltou a `false` (`conversa-ia.component.ts:146-156`). Nenhum caminho de gravação consulta `trocando()`; no 2D o `salvar()` só checa `!this.ponte` e o callback de ⌘S chama `guardar` direto. O backend (`SalvarArquivoFerramentaCommand`) sempre cria um arquivo novo, que vira o atual, sem conferir arquivo-base. Há ainda a brecha antes de `trocando` ligar: o `await getArquivo` no início de `trazerArquivo`.
- **Cenário:** a IA muda o modelo, aparece "Atualizando o modelo…", o joalheiro aperta ⌘S por hábito: toast "Modelo salvo no Castwork." e o atual no servidor passa a ser o modelo sem o que a IA fez; a tela mostra o da IA. A próxima mensagem, a conferência (peso e preço) e "Marcar como final" trabalham sobre o arquivo errado. O arquivo da IA continua no Histórico.
- **Correção:** um único `computed` de ocupado (`!!sincronizando() || pensando() || trocando()`) usado nas guardas de `salvar`, `abrirConferencia`, `montar`, `marcarComoFinal` e `restaurar`, nos `[disabled]` e no `[ocupado]` do histórico. Mover `trocando.set(true)` para a primeira linha de `trazerArquivo`, com o `getArquivo` dentro do `try`, e guardar a Promise da troca. `antesDeEnviar` deve aguardar essa Promise antes de avaliar `temAlteracoes()`, não simplesmente retornar. O callback de ⌘S ignora o salvamento quando ocupado ou quando a ponte que disparou não é mais `this.ponte`. Reforço opcional no servidor (M): o PUT enviar o id do arquivo-base e a API recusar com 409.
- **Esforço:** P.
- **Veredito:** CONFIRMADO nas duas ferramentas (testes com componente real e pontes simuladas: `putArquivo` recebe o conteúdo antigo durante a troca). Rebaixado de Alto para Médio pelos dois verificadores: janela de segundos e dano recuperável. Não provado: se o CADCraft exporta no ⌘S sem alterações pendentes.

#### M2 — Sintoma "Abrindo o modelo da versão…": nada no app recria o componente; a causa é recarga do documento ou ida e volta de rota

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:127-130`.
- **Trecho:** `if (this.ponte) return; this.ponte = this.criarPonte(0); this.sincronizando.set(... 'Abrindo o modelo da versão...');`
- **Problema:** o texto só é escrito em `iniciar()`, uma vez por instância. Conferido: rotas de topo, `App` sem `@if`, `cw-conversa-ia`/`cw-ferramenta-ia` projetam `[area]` fora de `@if`/`@for`, sem `RouteReuseStrategy` própria, sem `location.reload` do documento; o service worker do SolidCraft tem escopo `/solidcraft/` e não navega o topo. Ver a mensagem de novo exige instância nova.
- **Cenário (candidatos):** (1) recarga do documento: rebuild do `ng serve` em mudança de `.ts` (mudança só de template ou estilo entra por HMR), ou aba descartada por memória (dois iframes de um wasm de 134,8 MB); (2) refresh do token falhou e `auth.logout(router.url)` levou a `/login?returnTo` (A6, M4); (3) ida e volta de rota comum. Voltar/Avançar nas entradas de histórico dos iframes não recria o componente (ver M13).
- **Correção:** confirmar a causa antes de mexer. As mitigações de perda de trabalho pertencem ao A3. Trocar o projeto por `document.import` na mesma instância só depois de medir memória.
- **Esforço:** P para a instrumentação; M com mitigações.
- **Veredito:** PLAUSÍVEL. **Suspeita.** A parte estrutural está provada por teste (mesma instância e `carregar()` chamado 1 vez em três navegações na mesma rota; instância nova ao sair e voltar). A causa raiz não. Como confirmar: logar no constructor e em `DestroyRef.onDestroy` do componente e, no boot, `performance.getEntriesByType('navigation')[0].type` e `document.wasDiscarded`. Se o `onDestroy` não disparar antes do novo constructor, foi recarga do documento: testar com `ng serve --no-live-reload --no-hmr` ou build de produção; se o sintoma sumir, é ambiente de desenvolvimento e o item fecha. Se disparar, registrar `router.events` e olhar no Network se houve 401 seguido de refresh com erro.

#### M3 — Queda de rede no POST longo da conversa não retoma o turno: a fala some e a tela não acompanha a IA

- **Onde:** `frontend/src/app/modules/assistente/conversa-ia.component.ts:119-158` (catch em `:147`).
- **Trecho:** `} catch (erro) { if (!(erro instanceof HttpErrorResponse)) { this.toast.erro(...); } } finally { aoVivo?.unsubscribe(); this.pendente.set(null); this.andamento.set(null); this.pensando.set(false); }`
- **Problema:** o turno inteiro roda dentro de um único POST. No servidor a fala é gravada antes e o turno segue com `CancellationToken.None` (`ConversarFerramentaCommand.cs`). Se o POST falhar por rede ou timeout, o `catch` não relê a conversa nem entra em `aguardarIa()`, que só é alcançado por `carregar()` na criação do componente.
- **Cenário:** mensagem enviada no tablet; aos 40 s o Wi-Fi oscila. Toast "Sem conexão", a fala some do chat, `pensando=false`. A IA continua e grava um arquivo novo, mas `arquivoMudou` não é emitido: o editor segue com o arquivo antigo. Reenviar dá 422 "A IA ainda está trabalhando na mensagem anterior" (o backend barra o turno duplicado). Um Salvar nesse intervalo cria mais uma versão e gera divergência silenciosa (não destruição). Recarregar a página recupera; o próximo turno bem-sucedido também emite `arquivoMudou`.
- **Correção:** capturar `anterior` (o `arquivoId` de antes do envio) fora do `try`. No `catch` de `HttpErrorResponse` com status 0, 502, 503 ou 504, reler `getConversa`: se `processando`, aplicar a conversa e entrar numa variante `aguardarIa(anterior)` mantendo `pensando`; senão, aplicar a conversa e emitir `respondeu`, e `arquivoMudou` se `arquivoId !== anterior`. A releitura e o laço precisam tolerar erro e tentar de novo no intervalo de 4 s (com `BYPASS_ERROR` para não empilhar toasts), ver M19. O `finally` não zera `pendente`/`pensando` quando entra na retomada. Não tratar 4xx assim. Extrair um `sincronizar(anterior)` usado por `carregar`, pelo `catch` e pelo laço. Opcional no backend: `SalvarArquivoFerramentaCommand` recusar enquanto `AguardandoIa`.
- **Esforço:** M.
- **Veredito:** CONFIRMADO por dois verificadores (teste: após a falha, `getConversa` não é chamado, `mensagens()` vazio, `pensando()` falso, nenhum output emitido). Rebaixado de Alto para Médio: depende de falha de rede no meio do turno e nada se perde no servidor. O gatilho "504 de proxy" não foi provado (não há configuração de proxy no repositório).

#### M4 — `authGuard` descarta o refresh token válido quando o access token (15 min) vence

- **Onde:** `frontend/src/app/core/guards/auth.guard.ts:12-15`; `frontend/src/app/core/services/auth.service.ts:69-71`, `:90-94`.
- **Trecho:** `if (auth.isLoggedIn()) return true; auth.limparSessao(); return inject(Router).createUrlTree(['/login'], { queryParams: { returnTo: state.url } });`
- **Problema:** `isLoggedIn()` olha só o access token. O guard nunca tenta `renovarToken()` (único chamador em produção: o `jwtInterceptor`, depois de um 401). Com o access vencido ele apaga também o refresh de 7 dias e manda para o login. O guard roda em todo F5, na abertura do app e em toda travessia entre o layout e as rotas de tela cheia. Dentro do layout, navegar entre filhas não o reexecuta.
- **Cenário:** fechar a aba e voltar 20 minutos depois, ou dar F5: login, mesmo com refresh válido. Ler a página da versão por 16 minutos e clicar em "Modelo 3D": login. Modelar mais de 15 minutos sem requisição do Castwork e clicar em "Voltar": login.
- **Correção:** três casos no guard: logado devolve `true`; access vencido com refresh token devolve `auth.renovarToken().pipe(map(() => true), catchError(...))` (resolver `Router` e o UrlTree na parte síncrona); sem refresh token, limpar e ir ao login. Expor `temRefreshToken()` no `AuthService`. Em falha 400/401 limpar a sessão; em rede ou 5xx ir ao login sem apagar o refresh. Conferir que o `permissionGuard` lê as permissões do token já renovado. Em `guards.spec.ts`, manter o teste "deve redirecionar e limpar o storage quando o token venceu" só para o caso sem refresh e adicionar os casos novos. Opcional: renovação proativa antes do `exp`.
- **Esforço:** M.
- **Veredito:** CONFIRMADO (teste: access vencido + refresh válido termina em `/login?returnTo=%2Fpecas`, refresh apagado e nenhuma chamada HTTP). Rebaixado de Alto para Médio: sem perda de dados nem falha de segurança. Este achado não explica sozinho o sintoma do M2: aqui o usuário vê a tela de login.

#### M5 — Usuário sem `projetos.ler` fica preso no 403: início fixo em `/projetos` e página de erro sem menu nem "Sair"

- **Onde:** `frontend/src/app/app.routes.ts:74`; `frontend/src/app/layout/error-layout/pagina-erro/pagina-erro.component.ts:12`; `frontend/src/app/core/guards/convidado.guard.ts:7`.
- **Trecho:** `{ path: '', pathMatch: 'full', redirectTo: 'projetos' }`
- **Problema:** a raiz redireciona sempre para `projetos`, que exige `projetos.ler`; sem ela o `permissionGuard` devolve `/403`. O 403 fica no `ErrorLayoutComponent`, sem menu e sem `cw-usuario`; o único link volta para `/`. O `convidadoGuard` manda quem está logado de `/login` para `/`. O backend permite perfis com qualquer subconjunto de regras e usuário sem perfil.
- **Cenário:** perfil só com `pecas.ler` e `portfolio.ler` faz login: `/` vira `/projetos` e depois `/403`; "Voltar ao início" volta ao `/403`. Só sai digitando `/pecas` na barra de endereço ou limpando o storage.
- **Correção:** (1) em `PaginaErroComponent`, mostrar "Sair" quando `isLoggedIn()`; (2) trocar o `redirectTo` fixo por uma função que devolve a primeira rota de menu permitida (mesma fonte do menu: `montarMenu` + `PermissaoService`), ou `/403` se nenhuma; (3) mensagem própria para quem não tem nenhuma permissão; (4) casos em `app.routes.spec.ts` (token só com `pecas.ler` termina em `/pecas`; `permissoes: []` vê 403 com "Sair").
- **Esforço:** M (P só com os itens 1 e 2).
- **Veredito:** CONFIRMADO (teste com `appConfig.providers`). Rebaixado de Alto para Médio: admin nunca é afetado e há contorno.

#### M6 — Falha em download (`responseType: 'blob'`) é silenciosa: o interceptor ignora corpo Blob e ninguém mostra o erro

- **Onde:** serviços: `frontend/src/app/data/ferramentas/briefing/briefing.service.ts:30-35` (`getPrevia`, `getDocumento`), mais `AtelieService.getLogo`, `FerramentasService.getArquivo/getPreviaAtual/getPrevia/getImagemDoArquivo/getVersaoDoArquivo`, `PecasService.getDesenho/getPreviaDoDesenho/getImagemDoDesenho`. Chamadores: `modules/projetos/peca-do-projeto/peca-do-projeto.component.ts:328-336`; `modules/projetos/projeto-configuracoes/projeto-configuracoes.component.ts:114-135`; `modules/projetos/versao-detalhe/versao-detalhe.component.ts:223-227`; `modules/projetos/versao-detalhe/arquivos-versao.component.ts:446-459`, `:555-558`; `modules/pecas/desenho-da-peca/desenho-da-peca.component.ts:232`; `modules/ferramentas/briefing/briefing.component.ts:184`. Interceptor: `core/interceptors/erro-global.interceptor.ts:45`.
- **Trecho:** `const ignorar = ... || erro.error instanceof Blob;   // "quem baixou lê e mostra"`
- **Problema:** 11 dos 13 downloads da camada data não usam `lerErroBlob`, e nenhum chamador mostra o erro: assinam sem callback de erro ou fazem `await firstValueFrom(...)` sem try/catch. Mesmo `getFichaTecnicaDaPeca`, que aplica `lerErroBlob`, é assinado sem handler. O comentário em `arquivos-versao.component.ts:540` ("Erro da API já vira toast no interceptor") é falso para Blob. O `map((blob) => blob)` de `briefing.service.ts:35` não faz nada. Em `desenho-da-peca` e em `briefing.ver()` também não há estado "baixando" (duplo clique gera dois downloads) nem aviso quando o arquivo vem nulo (204).
- **Cenário:** clicar em "Baixar" a ficha técnica, o PDF do briefing, um arquivo da pasta da versão ou o desenho da peça e a API responder 404, 422, 500 ou 503: nada acontece na tela, nenhum toast, e fica uma rejeição não tratada no console.
- **Correção:** resolver num lugar só. Opção A: no `erroGlobalInterceptor`, quando `erro.error` for Blob, ler o texto (como `ler-erro-blob.operator.ts` já faz), montar o `HttpErrorResponse` com o JSON e mostrar o toast antes de relançar. Opção B: um helper único de download em `data/common` (ou `DownloadService.baixar(obs, nome)`) que aplica `lerErroBlob()` e chama `toast.erro(mensagemDeErroHttp(erro))`, usado em todos os pontos. Em qualquer opção, envolver os `firstValueFrom` em try/catch, adicionar um signal `baixando` nos botões e remover o `map` inócuo.
- **Esforço:** M.

#### M7 — Erro GraphQL com dado parcial vira "não encontrado" ou lista vazia

- **Onde:** `frontend/src/app/data/projetos/projetos/projetos.service.ts:328` (e `:336`, `:347`, `:355`, `:365`, `:412`, `:431`); `data/ferramentas/modelo-3d/modelo-3d.service.ts:71`; `data/pecas/pecas/pecas.service.ts:118`; listagens paginadas.
- **Trecho:** `.pipe(dadosGql(), map((dados) => dados.projeto))`
- **Problema:** com `errorPolicy: 'all'`, `dadosGql` só lança quando `data` inteiro é nulo. `projeto(id)`, `peca(id)` e as connections são anuláveis: se o resolver falha, a resposta é 200 com `data: { projeto: null }` e `errors`; o serviço devolve `null` e a tela entende "não existe". Em consultas aninhadas, um erro em campo não-nulo propaga o nulo até `projeto`.
- **Cenário:** o resolver de `projeto(id: 7)` lança: a tela mostra "Projeto não encontrado. Ele pode ter sido removido…" em vez do estado de erro. Na listagem, `data.projetos` nulo deixa a tela em branco. Em `getPesosDaVersao`, uma falha ao medir o 3D vira peso total calculado sobre um Map vazio.
- **Correção:** fazer `dadosGql` (ou uma variante) lançar quando houver `error` e o campo raiz pedido vier nulo; tratar connection nula como erro. Manter dado parcial só onde é desejado (campo opcional sob `@include`). Um caso de spec por serviço com `data` nulo + `errors`.
- **Esforço:** M.

#### M8 — Corrida `iniciar` × `trazerArquivo` prende o véu "Abrindo o modelo…" por até 60 s sobre o modelo já aberto

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:302`; `ponte-solidcraft.ts:85`, `:116`.
- **Trecho:** `const antiga = this.ponte; this.ponte = nova; this.ativo.set(atras); antiga?.descarregar();`
- **Problema:** `trazerArquivo` pode rodar enquanto `iniciar()` ainda espera `ponte.carregar()` no iframe A. Ao terminar, descarrega a ponte antiga com o `iniciar` ainda esperando nela: o laço `aguardar` consulta um `about:blank` até estourar 60 s, lança, e sai o toast falso "Não foi possível abrir o modelo no SolidCraft.". Nesse intervalo o véu cobre o modelo certo já carregado no iframe B. Se o `iniciar` estava no `await` do `document.import`, a Promise pode nunca resolver.
- **Cenário:** entrar no Modelo 3D com a IA ainda trabalhando num turno anterior e ela terminar antes de o SolidCraft subir; ou enviar uma mensagem enquanto o modelo abre (o chat não é bloqueado por `sincronizando`).
- **Correção:** a mesma fila do A2: `trazerArquivo` espera o `iniciar` terminar, ou o cancela com um AbortSignal que a ponte respeita. Ao descarregar uma ponte, marcá-la como descartada para o `aguardar` sair na hora, sem toast.
- **Esforço:** M.

#### M9 — Se o modelo não abre (timeout ou import falhou), o SolidCraft em branco fica salvável e sobrescreve o modelo

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:136`.
- **Trecho:** `} catch { this.toast.erro('Não foi possível abrir o modelo no SolidCraft.'); } finally { this.sincronizando.set(null); }`
- **Problema:** quando `ponte.carregar` falha, o componente só mostra o toast e libera a tela. `this.ponte` continua válido e o SolidCraft termina de subir com o projeto em branco; `salvar()` e `antesDeEnviar` gravam o vazio como versão atual. Em `trazerArquivo`, `atualId` já foi trocado (linha 296) antes de a troca dar certo: "Marcar como final" marca o arquivo novo, que não está à vista (igual no 2D).
- **Cenário:** primeira visita em rede lenta, o wasm leva mais de 60 s: toast de erro, depois o SolidCraft aparece vazio e sem véu; o joalheiro salva e a versão atual vira um projeto vazio (o anterior só volta pelo Histórico).
- **Correção:** guardar um estado `aberto` (ok/falhou). Em falha, manter um véu com "Tentar de novo" e bloquear salvar, `antesDeEnviar`, conferência e montar. Só atualizar `atualId` depois de a troca concluir. Trocar o polling com limite fixo pelo evento de pronto do SolidCraft, se existir.
- **Esforço:** M.

#### M10 — Abertura e troca do arquivo sem tratamento de erro: spinner eterno no 2D e falha silenciosa nas duas ferramentas

- **Onde:** `frontend/src/app/modules/ferramentas/desenho-2d/desenho-2d.component.ts:110` (`iniciar`), `:211` (`trazerArquivo`), `:190` (`baixar`); `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:295`.
- **Trecho:** `const arquivo = await firstValueFrom(this.ferramentas.getArquivo(...)); this.atualId.set(arquivo?.id ?? null); ... try {`
- **Problema:** o `getArquivo` (Blob, sem toast do interceptor) fica fora do `try`. No 2D, `iniciar()` não tem try/finally: `sincronizando` nasce com "Abrindo o CADCraft..." e nunca é zerado se o GET rejeitar; `ponte` já definido impede nova tentativa. Nas duas, `arquivoMudou()` faz `void this.trazerArquivo()` (rejeição solta) e os `catch` de `restaurar`, `montar` e `marcarComoFinal` calam de propósito para `HttpErrorResponse` ("o interceptor já avisou"), o que aqui não é verdade; os toasts de sucesso são pulados embora a operação tenha sido gravada.
- **Cenário:** abrir o Desenho 2D com o GET `/arquivo` em 500, 403 ou sem rede: overlay para sempre, todos os botões desabilitados, nenhum toast. Depois de um turno da IA, o GET falha em silêncio: o chat mostra "Aplicou: …" e a prancheta continua com o arquivo antigo; o joalheiro salva e sobrescreve o que a IA fez.
- **Correção:** envolver `iniciar()` do 2D em try/catch/finally como no 3D; mover o `getArquivo` para dentro do `try` de `trazerArquivo` com toast próprio e "tentar de novo", sem propagar erro HTTP para quem chamou; marcar a tela como desatualizada (bloqueando salvar) até a troca dar certo.
- **Esforço:** P.

#### M11 — Confirmação de peça publicada aparece atrás do diálogo modal do Histórico

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:243`; `modelo-3d.component.html:38`; `historico-modelo.component.ts:151`.
- **Trecho:** `if (!this.daPeca() || this.sincronizando() || this.pensando() || !(await this.avisoPublicada().confirmar())) return;`
- **Problema:** `marcarComoFinal($event.id)` vem do Histórico, um `<dialog>` aberto com `showModal()`: o resto do documento fica inerte. A confirmação (`cw-aviso-peca-publicada`) é renderizada na prancheta, fora do diálogo, e o `historico.set(null)` só acontece depois do POST. Depois do `await confirmar()` as guardas não são conferidas de novo.
- **Cenário:** peça publicada, Histórico, "Marcar como final" numa versão antiga: nada acontece visivelmente. Ao fechar o Histórico a pergunta fica clicável e, se confirmada, marca aquela versão, talvez minutos depois e com a IA já trabalhando.
- **Correção:** fechar o Histórico antes de pedir a confirmação (ou renderizá-la como diálogo modal próprio) e reavaliar as guardas depois do `await`. Conferir o mesmo fluxo no Desenho 2D, que é cópia.
- **Esforço:** P.

#### M12 — Ferramentas e chat da IA oferecem escrita a quem só tem `projetos.ler`

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.html:32`; `frontend/src/app/modules/ferramentas/desenho-2d/desenho-2d.component.html:18-21`; `frontend/src/app/modules/ferramentas/briefing/briefing.component.html:90`; rotas em `app.routes.ts:22-64`.
- **Trecho:** `<button ... [disabled]="!!sincronizando() || pensando()" (click)="salvar()">Salvar</button>`
- **Problema:** as rotas exigem só `projetos.ler`, mas salvar arquivo, restaurar, conversar, desfazer e apagar conversa exigem `projetos.editar` no backend. `podeEditar` só esconde "Montar", "Marcar como final" e, no briefing, "Formulário". Salvar, ⌘S, "Restaurar" do histórico, o compositor, "Novo chat", "Apagar" e "Desfazer" ficam ativos; `cw-conversa-ia`/`cw-ferramenta-ia` não têm modo somente leitura. No 3D, `salvar()` e `abrirConferencia()` não filtram `HttpErrorResponse` (dois toasts), e `abrirConferencia()` tenta salvar antes de abrir.
- **Cenário:** usuário só-leitura abre o Desenho 2D, desenha 20 minutos e clica em Salvar: 403 e o trabalho não tem como ser guardado. No 3D, move um corpo e clica em "Peso e materiais": dois toasts e a conferência, que é leitura, não abre. No chat, a mensagem enviada volta 403 e o texto digitado é apagado (M16).
- **Correção:** input `somenteLeitura` em `ConversaIaComponent`/`FerramentaIaComponent` (esconde compositor, anexos, Novo chat, Apagar e Desfazer; conversas anteriores só para leitura). Condicionar Salvar, Restaurar e ⌘S a `podeEditar()` nas duas ferramentas, com aviso "somente leitura" na prancheta. Em `abrirConferencia`, `antesDeEnviar` e `restaurar`, só salvar quando `podeEditar()`.
- **Esforço:** M.

#### M13 — Trocar `iframe.src` cria entradas no histórico do navegador: Voltar apaga o modelo à vista

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/ponte-solidcraft.ts:72-79`; mesmo padrão em `desenho-2d/ponte-cadcraft.ts:95-100`.
- **Trecho:** `descarregar(): void { this.iframe.src = 'about:blank'; }`
- **Problema:** navegar um iframe por `src` depois da carga inicial adiciona uma entrada ao histórico conjunto da aba (1 por abertura, 2 por troca). O Voltar do navegador passa a desfazer essas navegações: o iframe ativo volta para `about:blank` e `this.ponte` aponta para janela vazia.
- **Cenário:** abrir o Modelo 3D, pedir uma mudança à IA, apertar Voltar duas vezes: a prancheta fica vazia sem aviso e Salvar responde "Não foi possível salvar o modelo.".
- **Correção:** navegar sem criar histórico com `iframe.contentWindow.location.replace(url)`, ou recriar o elemento iframe a cada troca.
- **Esforço:** P.
- **Suspeita.** Como confirmar: no Chrome e no Safari do tablet, conferir `history.length` antes e depois de abrir a tela e de uma troca, e apertar Voltar observando o `src` dos dois iframes.

#### M14 — Chamadas ao SolidCraft sem timeout: se a API não responder, o véu "Guardando…" fica preso

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/ponte-solidcraft.ts:55-60`.
- **Trecho:** `const projeto = await api.request<ProjetoExportado>('document.export');`
- **Problema:** só o `aguardar` da carga tem limite. `temAlteracoes`, `salvar`, `corposSemMaterial` e o `document.import` esperam `api.request` indefinidamente, com `sincronizando` ligado e zerado só no `finally`. Além disso, `temAlteracoes()` faz um `document.export` completo só para ler `dirty`, e o salvar exporta de novo.
- **Cenário:** Salvar com o SolidCraft travado num cálculo pesado, ou logo após o iframe ser esvaziado por uma corrida de trocas: "Guardando o modelo…" permanente; única saída é o F5.
- **Correção:** envolver `request` num helper com timeout (`Promise.race`) e erro claro; um único `exportar()` que devolve `{ dirty, text, revision }` reutilizado entre `temAlteracoes` e `salvar`.
- **Esforço:** P.
- **Suspeita.** Como confirmar: teste da ponte com `window.solidcraft.request` devolvendo uma Promise que nunca resolve; verificar na documentação da API do SolidCraft se `request` tem timeout próprio.

#### M15 — Duplicação extensa entre Desenho 2D e Modelo 3D, já com divergências que viraram bugs

- **Onde:** `frontend/src/app/modules/ferramentas/desenho-2d/desenho-2d.component.ts:40` × `modelo-3d/modelo-3d.component.ts:44`; `historico-desenho.component.ts` × `historico-modelo.component.ts:132`; os dois `.scss` e `.html`.
- **Trecho:** `export class Desenho2dComponent {  // 16 campos e 11 métodos repetidos em Modelo3dComponent`
- **Problema:** cerca de 200 linhas quase idênticas de orquestração (escopo/peça/finais, `sincronizando`/`trocando`/`ativo`, `antesDeEnviar`, `salvar`, `restaurar`, `marcarComoFinal`, `baixar`, `guardar`, `trazerArquivo`, `emBase64`). Os dois históricos têm 159 linhas e diferem só em rótulos; os `.scss` têm 72 linhas e diferem em 5. A cópia já divergiu: `restaurar()` do 2D não zera `sincronizando` (A4), `iniciar()` do 2D não tem try/finally (M10), `salvar()` e o ⌘S do 2D não têm guardas (M1); `temAlteracoes()` é síncrono no 2D e assíncrono no 3D.
- **Cenário:** corrigir a fila de trocas só no 3D deixa o 2D com o mesmo bug dos dois iframes em branco.
- **Correção:** extrair uma base comum ("arquivo da ferramenta": fila de operações, estado ocupado, final da peça), parametrizada por código da ferramenta, extensão, textos e uma interface de ponte `{ carregar, temAlteracoes, salvar, descarregar }`; um único `cw-historico-arquivo` com rótulos por input; mover `emBase64` e o `.scss` da prancheta para o lugar comum. Ver seção 3.
- **Esforço:** G.

#### M16 — Texto e anexos da mensagem são apagados antes de saber se o envio deu certo; textarea sem limite de 4000 caracteres

- **Onde:** `frontend/src/app/shared/ui/ferramenta-ia/ferramenta-ia.component.ts:443-449`; `frontend/src/app/modules/assistente/conversa-ia.component.ts:153`.
- **Trecho:** `this.enviar.emit({ texto, imagens: this.anexos(), arquivos: this.arquivos(), ...this.escolha() }); this.fala = ''; this.anexos.set([]); this.arquivos.set([]);`
- **Problema:** o compositor limpa tudo no instante em que emite. Em qualquer falha (o `antesDeEnviar` não conseguiu guardar o arquivo, 422, 400 de texto acima de 4000 caracteres, 503 da IA, 403, rede), o `ConversaIaComponent` só remove o `pendente`; nada devolve o conteúdo. O backend valida `MaximumLength(4000)` e a textarea não tem `maxlength`.
- **Cenário:** o joalheiro cola um briefing de 5000 caracteres com 3 fotos e envia: 422, toast, e a caixa de texto e as miniaturas já estão vazias.
- **Correção:** limpar só após sucesso: `enviar` devolve `Promise<boolean>`, ou o pai reinjeta a fala por um input `rascunho` no erro. Adicionar `maxlength="4000"`.
- **Esforço:** M.

#### M17 — `PonteCadCraft.salvar()`: depois de um timeout, `esperandoSalvar` fica preso e o próximo ⌘S é engolido

- **Onde:** `frontend/src/app/modules/ferramentas/desenho-2d/ponte-cadcraft.ts:49`, `:82`.
- **Trecho:** `this.esperandoSalvar = resolve; setTimeout(() => reject(new Error('O CADCraft não salvou o desenho.')), 10_000);`
- **Problema:** quando o prazo de 10 s estoura, a Promise é rejeitada, mas `esperandoSalvar` continua apontando para o `resolve` morto e o timer nunca é limpo. No próximo salvamento feito dentro do CADCraft, o Blob vai para esse `resolve` em vez de `aoSalvar`.
- **Cenário:** Salvar pelo Castwork estoura o tempo; o joalheiro aperta ⌘S dentro do CADCraft: o CADCraft considera o arquivo salvo, nada é enviado ao Castwork e não aparece toast.
- **Correção:** no timeout, zerar `esperandoSalvar` se ainda for o `resolve` desta chamada; limpar o timer ao resolver; rejeitar a chamada anterior se `salvar()` for chamado de novo.
- **Esforço:** P.

#### M18 — Falha ao gravar no Castwork depois que o CADCraft já considerou o desenho salvo: as alterações à mão deixam de ser enviadas

- **Onde:** `frontend/src/app/modules/ferramentas/desenho-2d/desenho-2d.component.ts:233`; `ponte-cadcraft.ts:41-43`.
- **Trecho:** `temAlteracoes(): boolean { return this.iframe.contentDocument?.title.includes('•') ?? false; }`
- **Problema:** o único sinal de "há alterações não enviadas" é o "•" no título do CADCraft, que some quando ele conclui o download interceptado. Se o `putArquivo` falhar depois disso, o ⌘S usa `void this.guardar(...)` sem `catch` e nada marca o desenho como pendente.
- **Cenário:** desenhar, apertar ⌘S, o PUT falha. Ao mandar mensagem à IA, `antesDeEnviar` não salva (sem "•"); a IA trabalha no arquivo antigo e a tela troca para o resultado dela. O desenho à mão é descartado sem nunca ter sido gravado.
- **Correção:** um flag próprio `pendenteDeEnvio` (ligado quando a ponte entrega um Blob, desligado só após o PUT responder), usado junto com `temAlteracoes()`; guardar o último Blob para reenviar; toast específico no erro do ⌘S.
- **Esforço:** M.
- **Suspeita.** Como confirmar: verificar no CADCraft embutido que o "•" some após ⌘S mesmo com o download interceptado; depois bloquear o PUT `.../desenho-2d/arquivo`, apertar ⌘S e enviar uma mensagem: conferir que nenhum PUT é refeito antes do POST `/conversa`.

#### M19 — Conversa com IA: carga inicial e polling sem tratamento de erro (um GET falho encerra o acompanhamento)

- **Onde:** `frontend/src/app/modules/assistente/conversa-ia.component.ts:198-210` (laço em `:203`), `:106`.
- **Trecho:** `const conversa = await firstValueFrom(this.ferramentas.getConversa(...));` dentro do `while`, sem `catch`; `untracked(() => void this.carregar(projetoId, escopo, ferramenta));`
- **Problema:** `aguardarIa()` não tem `catch` por iteração: a primeira consulta que falhar derruba o laço, o `finally` desliga `pensando` e o turno segue no servidor por até 15 minutos. `carregar()` é disparado com `void` e sem `catch`: se o GET inicial falhar, o chat aparece vazio com as boas-vindas, sem "tentar de novo". `carregar` também não descarta resposta velha se `projetoId`/`escopo` mudarem com o componente reaproveitado.
- **Cenário:** voltar à tela no meio de um turno; na terceira consulta a rede oscila. O chat sai de "Trabalhando", os botões voltam e, quando a IA termina, nem a resposta nem o arquivo novo aparecem. Salvar nesse intervalo grava o arquivo antigo como atual.
- **Correção:** try/catch dentro do laço, continuando com recuo e saindo só quando `processando=false` ou o componente for destruído. Em `carregar`, estado de erro com "Tentar de novo" e um token de requisição para ignorar respostas de parâmetros antigos e cancelar o laço anterior.
- **Esforço:** M.

#### M20 — Anexos de imagem: limite calculado antes dos `await` (passa de 4) e envio possível antes de a imagem ficar pronta

- **Onde:** `frontend/src/app/shared/ui/anexos-imagem/anexos-imagem.component.ts:85-90`, `:17`.
- **Trecho:** `const livres = this.maximo() - this.imagens().length; ... const imagem = await prepararImagem(arquivo); this.imagens.update((lista) => [...lista, imagem]);`
- **Problema:** `livres` é lido uma vez, antes do processamento assíncrono (HEIC leva segundos); chamadas sobrepostas de `adicionar()` enxergam a mesma folga. Não há estado "preparando": Enviar e Enter leem `anexos()` no instante do envio. `track imagem.url` usa a data URL inteira como chave (a mesma foto duas vezes duplica a chave), e arquivos que não são imagem são descartados em silêncio.
- **Cenário:** colar 3 fotos HEIC e, antes de terminarem, arrastar mais 3: a lista fica com 6; a API devolve 422 "Anexe até 4 imagens" e o texto digitado é perdido (M16). Escolher uma foto HEIC e apertar Enter: a mensagem vai sem a imagem, e a miniatura aparece depois, no compositor da próxima.
- **Correção:** recalcular o espaço livre dentro do `update`; signal `preparando` que desabilita Enviar e mostra um placeholder; id incremental por anexo para o `track`; avisar quando arquivos forem ignorados por tipo.
- **Esforço:** P.

#### M21 — "Apagar este chat" pode ser disparado com a IA trabalhando

- **Onde:** `frontend/src/app/shared/ui/ferramenta-ia/ferramenta-ia.component.ts:87` (menu ⋯, sem `[disabled]`) e `:127` (botão do topo, com `[disabled]="pensando() || ..."`); `frontend/src/app/modules/assistente/conversa-ia.component.ts:182`.
- **Trecho:** `<button type="button" role="menuitem" class="perigo" (click)="pedirApagar()">… Apagar este chat</button>`
- **Problema:** as duas entradas para a mesma ação têm regras diferentes, e `pedirApagar()`/`apagar()` não checam `pensando()`. O backend também não barra: `ApagarConversaFerramentaCommand` não chama `AguardandoIa`, ao contrário do desfazer.
- **Cenário:** com a IA trabalhando, ⋯ → Apagar este chat → Apagar: as mensagens somem e, quando o turno termina, `conversa.set(resposta)` traz o chat de volta; o estado final no banco depende da ordem das duas gravações.
- **Correção:** aplicar o mesmo `[disabled]` no item do menu e sair cedo em `apagar()`, `novoChat()`, `abrirChat()` e `desfazer()` quando `pensando()`; recusar no backend com `AguardandoIa`.
- **Esforço:** P.
- **Suspeita** quanto à consequência no servidor (a parte do front está provada pela leitura: a requisição é enviada). Como confirmar: apagar pelo menu ⋯ durante um turno e conferir a resposta do POST da conversa e o histórico do arquivo; ou um teste de integração chamando Apagar entre `RegistrarFala` e `ConfirmarFala`.

#### M22 — Criar projeto e trocar cliente dependem de `clientes.ler`, que a tela não exige

- **Onde:** `frontend/src/app/modules/projetos/novo-projeto/novo-projeto.component.ts:27` e `.html:12`; `projeto-configuracoes/projeto-configuracoes.component.ts:50`.
- **Trecho:** `protected readonly clientes = rxResource({ stream: () => this.clientesService.getClientesOpcoes() });`
- **Problema:** a rota `novo` pede só `projetos.criar` e as configurações liberam o seletor com `projetos.editar`, mas a consulta `clientes` exige a policy `clientes.ler`. Sem ela o recurso entra em erro e `clientes.value() ?? []` lança (A1).
- **Cenário:** perfil com `projetos.criar` e sem `clientes.ler` abre `/projetos/novo`: toast de autorização e o template lança no `@for`; descrição e botão Criar não são atualizados.
- **Correção:** pedir as opções só com `temAlguma(['clientes.ler'])` (senão `of([])` e seletor desabilitado com uma dica) e ler o recurso com guarda.
- **Esforço:** P.

#### M23 — Peça do projeto: definir valor ou publicar descarta, sem aviso, as edições não salvas do formulário de dados

- **Onde:** `frontend/src/app/modules/projetos/peca-do-projeto/peca-do-projeto.component.ts:193-196`.
- **Trecho:** `effect(() => { const peca = this.peca.value(); if (peca) this.preencher(peca); });`
- **Problema:** o effect repreenche o formulário toda vez que `peca.value()` muda de objeto. `salvarValor()` e `publicar()` chamam `peca.reload()`, e a peça volta como objeto novo: `form.reset(...)` com os dados do servidor. Em Publicar, publica-se o estado salvo, não o que está na tela.
- **Cenário:** trocar o nome e o peso sem salvar, digitar o custo em "Valor no projeto" e clicar em "Definir valor": nome e peso voltam ao do servidor, sem aviso.
- **Correção:** repreencher só na primeira carga e depois do próprio `salvar()` (ou `if (!this.form.dirty)`); após `salvarValor()` atualizar apenas `this.valor`; em `publicar()`, bloquear ou confirmar quando `form.dirty`.
- **Esforço:** P.

#### M24 — Pasta de arquivos da versão: carga sem tratamento de erro (spinner eterno) e corrida ao trocar de aba

- **Onde:** `frontend/src/app/modules/projetos/versao-detalhe/arquivos-versao.component.ts:465`.
- **Trecho:** `this.pagina.set(null); void this.carregar(1);`
- **Problema:** `void this.carregar(1)` em `abrir()`, `trocarAba()` e na paginação não trata rejeição: se a listagem falhar, o modal fica em "Carregando..." para sempre. `carregar` lê `arquivados()` na chamada e grava `pagina` na resposta sem conferir se a aba ainda é a mesma.
- **Cenário:** clicar em "Arquivados" (resposta lenta) e logo em "Arquivos" (rápida): quando a primeira resposta chega, a aba "Arquivos" lista os arquivados, com Renomear/Abrir/Arquivar para arquivos já arquivados.
- **Correção:** `rxResource` com `params: () => ({ aberto, arquivados, pagina })` (cancela a anterior e dá estado de erro), ou no mínimo try/catch com estado de erro e uma sequência para descartar respostas antigas.
- **Esforço:** M.

#### M25 — Pasta de arquivos oferece renomear, arquivar e apagar a quem só tem `projetos.ler`

- **Onde:** `frontend/src/app/modules/projetos/versao-detalhe/arquivos-versao.component.ts:138`; `versao-detalhe.component.html:108`.
- **Trecho:** `<cw-arquivos-versao [projetoId]="+id()" [versaoId]="v.id" />`
- **Problema:** o componente não recebe nenhuma permissão; os commands exigem `projetos.editar` no backend. O resto da página esconde as ações com `podeEditar`.
- **Cenário:** usuário de consulta abre Arquivos, vê lápis, arquivar e lixeira; confirma "Apagar de vez? Não dá para desfazer" e recebe 403.
- **Correção:** `readonly podeEditar = input(false)`, envolver as ações de escrita em `@if (podeEditar())` e passar `[podeEditar]="podeEditar"`.
- **Esforço:** P.

#### M26 — Stepper de quantidade: gravações concorrentes e número não salvo permanecendo na tela após falha

- **Onde:** `frontend/src/app/modules/projetos/versao-detalhe/versao-detalhe.component.ts:150`, `:219`; `frontend/src/app/shared/ui/quantidade.component.ts:74`.
- **Trecho:** `protected readonly atual = linkedSignal(() => this.valor());` e `error: () => this.projeto.reload(),`
- **Problema:** (1) no erro, a tela conta com `projeto.reload()` para desfazer, mas o reload devolve a mesma quantidade; o input não notifica (igualdade de primitivo) e o stepper continua mostrando o número que não foi gravado. (2) `executar` não impede reentrada: o temporizador de 500 ms de outro stepper já tocado emite com um PUT em andamento, e o `finalize` do primeiro zera `salvando` com o segundo em voo.
- **Cenário:** item com 2 un.; tocar em "+" e o PUT falhar: o campo mostra 3, o resumo "· 2 un." e o servidor tem 2. Tocar "+" em dois itens em menos de 500 ms: dois PUT simultâneos.
- **Correção:** serializar as gravações (fila com `concatMap`, ou reenfileirar enquanto `salvando()`), contador em vez de booleano, e dar ao `cw-quantidade` um jeito de reverter (método `reverter()` ou `model()` com set explícito do pai). Cobrir com teste em que o pai não altera `valor` após `alterado`.
- **Esforço:** M.

#### M27 — `cw-quantidade` descarta a alteração pendente quando é destruído antes do debounce

- **Onde:** `frontend/src/app/shared/ui/quantidade.component.ts:78`.
- **Trecho:** `inject(DestroyRef).onDestroy(() => clearTimeout(this.temporizador));`
- **Problema:** a emissão de `alterado` espera 500 ms; ao destruir, o temporizador é só cancelado. A mudança que o usuário viu na tela nunca é gravada.
- **Cenário:** tocar em "+" três vezes e, em menos de 500 ms, tocar em "Ver 2D" ou em voltar: nenhum PUT sai.
- **Correção:** no `onDestroy`, se há temporizador pendente e `atual() !== valor()`, emitir antes de limpar; ou emitir também no `blur`.
- **Esforço:** P.

#### M28 — Parâmetros de preço: edição não salva é apagada quando outro bloco da página salva

- **Onde:** `frontend/src/app/modules/precificacao/parametros-preco.component.ts:99`.
- **Trecho:** `effect(() => { this.preencher(this.parametros()); if (this.podeEditar()) this.form.enable(); else this.form.disable(); });`
- **Problema:** o effect faz `form.reset(...)` sempre que o input `parametros` muda de referência. A página chama `precos.reload()` depois de salvar cotação ou liga, e cada consulta `network-only` devolve um objeto `parametros` novo (conferido com o Apollo Client instalado).
- **Cenário:** digitar markup 2,0 sem salvar, atualizar a cotação do ouro e salvar: o formulário de parâmetros volta em silêncio para 1,8.
- **Correção:** só repor quando o formulário estiver pristine ou quando os valores mudarem de fato (`computed` com `equal` estrutural). Alternativa: cada bloco reler só a sua parte.
- **Esforço:** P.

#### M29 — Visualizador de desenho: falha ao trocar de vista deixa a imagem da vista anterior sob o botão da nova

- **Onde:** `frontend/src/app/modules/pecas/desenho-da-peca/desenho-da-peca.component.ts:172`.
- **Trecho:** `this.vista.set(vista); ... const blob = await firstValueFrom(this.imagemGuardada(vista)); if (!blob) return;`
- **Problema:** `vista` é trocada antes da carga e `imagem` não é limpa. Se a nova imagem falhar ou vier 204, `imagem()` continua com a URL anterior, e o template só mostra `estado()` quando `imagem()` é nulo.
- **Cenário:** Iso carrega; clicar em "Frente" e o GET devolver 500: o botão "Frente" fica ativo, a imagem continua sendo a Iso e "Baixar imagem" fica habilitado sem efeito.
- **Correção:** limpar `imagem` (ou voltar à vista anterior) na falha e mostrar o estado de erro; em `abrir`, `vista.set(null)` ao cair na prévia; habilitar "Baixar imagem" por `originais.has(vista)`.
- **Esforço:** P.

#### M30 — Briefing: o visualizador de PDF é destruído e recriado, e o PDF é pedido duas vezes, a cada resposta da IA

- **Onde:** `frontend/src/app/modules/ferramentas/briefing/briefing.component.ts:78-83`, `:166`.
- **Trecho:** `return b ? { projetoId: b.projetoId, documentoId: b.documentos[0]?.id ?? null } : undefined;`
- **Problema:** o `params` devolve um objeto novo sempre que `briefing.value()` muda de identidade; o resource compara por referência, zera o valor e vai a `loading`. `@if (pdfAtual.hasValue())` fica falso e o `<cw-visualizador-pdf>` é destruído. Em `aposResposta`, o `pdfAtual.reload()` explícito dispara um download e a chegada do briefing dispara outro.
- **Cenário:** o joalheiro lê a página 2 do PDF com zoom; a IA responde a uma fala qualquer e o visualizador some e volta do início. Duas requisições de PDF por turno.
- **Correção:** `computed` com `equal` comparando `projetoId` e `documentoId` (ou params primitivo) e, em `aposResposta`, recarregar o briefing e só então `pdfAtual.reload()` uma vez (o reload mantém o valor anterior).
- **Esforço:** P.

#### M31 — Sessão sem coordenação entre abas: corrida de refresh derruba a família inteira e o token em memória fica defasado

- **Onde:** `frontend/src/app/core/services/auth.service.ts:59`.
- **Trecho:** `this.refreshEmAndamento$ ??= this.refreshTokenService.postRefreshToken({ refreshToken }).pipe(`
- **Problema:** `refreshEmAndamento$` deduplica só dentro de um contexto JS, mas o refresh token fica no localStorage compartilhado. O backend faz rotação com detecção de reuso: duas abas que renovam com o mesmo token fazem a segunda chamada revogar a família. Não há listener de `storage`: o signal `_token` de uma aba não acompanha login ou logout feito em outra.
- **Cenário:** duas abas e o notebook volta da suspensão com o access vencido: as duas enviam `auth/refresh` com o mesmo token; a segunda recebe 401, chama `logout()` e apaga os tokens que a primeira gravou. Ou: logout na aba A e login como outro usuário; a aba B segue enviando o token anterior por até 15 minutos.
- **Correção:** serializar o refresh com `navigator.locks.request('castwork.refresh', ...)` e, dentro do lock, reler o storage (se o access já foi renovado, só adotá-lo). Ouvir `storage` para sincronizar `_token`, deslogando ou recarregando quando o usuário mudar.
- **Esforço:** M.

#### M32 — Access token e refresh token (7 dias) guardados em localStorage

- **Onde:** `frontend/src/app/core/services/auth.service.ts:85-86`.
- **Trecho:** `this.storage.definir(CHAVE_ACCESS_TOKEN, accessToken); this.storage.definir(CHAVE_REFRESH_TOKEN, refreshToken);`
- **Problema:** os dois tokens ficam legíveis por qualquer script da origem, e o SolidCraft e o CADCraft (código de terceiro versionado fora do git, em `public/`) rodam na mesma origem. O refresh basta para manter uma sessão fora do navegador da vítima; a detecção de reuso só age se a vítima usar o token antigo depois.
- **Cenário:** um XSS ou uma dependência comprometida lê `localStorage['castwork.refreshToken']` e passa a ter acesso contínuo com as permissões do usuário. Nenhum vetor de XSS foi encontrado na revisão.
- **Correção:** refresh token em cookie `HttpOnly; Secure; SameSite=Strict` restrito a `/api/auth` e access token só em memória, com refresh silencioso no F5. Se o localStorage for decisão de produto, registrá-la e compensar com CSP restritiva e refresh de vida mais curta.
- **Esforço:** G.

#### M33 — `erroGlobalInterceptor` descarta o `detail` do servidor fora de 400/422

- **Onde:** `frontend/src/app/core/interceptors/erro-global.interceptor.ts:29`.
- **Trecho:** `if ((erro.status === 400 || erro.status === 422) && ehProblemDetails(corpo)) {`
- **Problema:** o backend devolve `detail` seguro em 403, 404, 409 e 503, e no 500 pede o código de rastreio. O interceptor só usa o corpo em 400 e 422; o 429 cai em "Não foi possível concluir a requisição", sem usar o `Retry-After`.
- **Cenário:** a IA está fora e a API responde 503 com a explicação: o toast mostra "Erro no servidor". Dois envios simultâneos dão 409 e o toast diz que o registro foi alterado por outra pessoa.
- **Correção:** em `mensagemDeErroHttp`, para qualquer status diferente de 401 com ProblemDetails, preferir `corpo.detail`; incluir o `traceId` no 500; mensagem própria no 429. Testes por status.
- **Esforço:** P.

#### M34 — Sem `ErrorHandler` global nem tratamento de erro de navegação

- **Onde:** `frontend/src/app/app.config.ts:25`.
- **Trecho:** `provideBrowserGlobalErrorListeners(), provideZonelessChangeDetection(), provideRouter(routes, withComponentInputBinding()),`
- **Problema:** o `core/README.md` anuncia um `GlobalErrorHandler`, mas `core/error-handlers/` só tem `.gitkeep`. O router não usa `withNavigationErrorHandler`. Todas as features são lazy: se o import do chunk falhar (deploy novo com a aba antiga aberta), a navegação é cancelada e o erro vai só para o console. O mesmo vale para as muitas rejeições de promessa sem `catch` deste relatório.
- **Cenário:** o app é atualizado com o tablet do ateliê aberto; o usuário toca em "Precificação" e nada acontece.
- **Correção:** criar o `GlobalErrorHandler` (toast genérico e log) e usar `withNavigationErrorHandler` para detectar falha de import dinâmico e oferecer recarregar. Enquanto não existir, corrigir o README.
- **Esforço:** M.

#### M35 — `cw-imagem-protegida` não trata falha de carregamento: fica em "carregando" para sempre

- **Onde:** `frontend/src/app/shared/ui/imagem-protegida.component.ts:105`.
- **Trecho:** `this.http.get(src, { responseType: 'blob' }).subscribe((blob) => this.url.set(URL.createObjectURL(blob)));`
- **Problema:** o subscribe não tem callback de erro e o componente não tem estado de erro; por ser Blob, o interceptor também não avisa (mesma causa do M6).
- **Cenário:** abrir uma conversa cuja imagem foi apagada (404), sem permissão ou offline: a miniatura fica cinza para sempre, com o botão desabilitado.
- **Correção:** signal `falhou`, tratar `error`, mostrar "Imagem indisponível" com `aria-label` coerente e toque para tentar de novo; zerar `url` e `falhou` ao trocar `src`.
- **Esforço:** P.

#### M36 — `cw-visualizador-pdf`: troca de arquivo durante a renderização mistura páginas de dois PDFs

- **Onde:** `frontend/src/app/shared/ui/visualizador-pdf.component.ts:82`.
- **Trecho:** `effect(() => void this.renderizar(this.arquivo()));`
- **Problema:** a única guarda de cancelamento é `destruido`. Quando o input muda com o componente vivo, o laço anterior continua anexando canvas no mesmo contêiner; o `finally` antigo faz `carregando.set(false)` com a nova renderização em curso.
- **Cenário:** no briefing, `pdfAtual.reload()` entrega outro Blob com o componente mantido; se chega enquanto o anterior ainda é desenhado, a folha fica com páginas intercaladas.
- **Correção:** contador de geração (ou AbortController) checado a cada iteração e antes de cada `append`/`set`; cancelar a tarefa anterior no `onCleanup` do effect; try/catch no `destroy` do `finally`.
- **Esforço:** M.

#### M37 — `cw-visualizador-pdf` renderiza todas as páginas de uma vez em alta escala

- **Onde:** `frontend/src/app/shared/ui/visualizador-pdf.component.ts:81`.
- **Trecho:** `const escala = Math.min(window.devicePixelRatio || 1, PIXELS_POR_PONTO_MAXIMO) * 1.5;`
- **Problema:** a escala efetiva é DPR × 1,5 (até 4,5). Uma página A4 em iPad (DPR 2) vira cerca de 18 MB de bitmap, e todas ficam vivas no DOM. O Safari do iOS limita a memória total de canvas.
- **Cenário:** PDF de 20 páginas no iPad: cerca de 360 MB de canvas; as últimas páginas aparecem em branco ou a aba é recarregada (o que também reproduziria o sintoma do M2 em outras telas).
- **Correção:** limitar a escala final (por exemplo `min(DPR, 2)`) e renderizar sob demanda com IntersectionObserver.
- **Esforço:** M.
- **Suspeita.** Como confirmar: abrir um PDF de 20 a 30 páginas em um iPad real e observar o console ("Total canvas memory use exceeds the maximum limit") e páginas em branco; conferir o tamanho típico dos PDFs gerados.

#### M38 — Downloads revogam a object URL cedo demais (`DownloadService` e duas cópias manuais)

- **Onde:** `frontend/src/app/shared/services/download.service.ts:16`; cópias manuais com revogação síncrona em `frontend/src/app/modules/projetos/projeto-configuracoes/projeto-configuracoes.component.ts:126` e `peca-do-projeto/peca-do-projeto.component.ts:319-326`.
- **Trecho:** `link.click(); setTimeout(() => URL.revokeObjectURL(url));`
- **Problema:** o serviço revoga a URL em 0 a 4 ms; as duas cópias manuais revogam de forma síncrona logo após o `click()`. O Safari (alvo declarado: tablet) inicia o download de blob de forma assíncrona. O `<a>` também não é inserido no DOM.
- **Cenário:** no iPad, baixar o STL ou DXF grande ou o PDF do briefing: o download falha ou não inicia, sem mensagem.
- **Correção:** revogar com folga (30 a 60 s) e anexar/remover o link do body em volta do click; usar `DownloadService.salvar` nos dois pontos manuais; criar teste do serviço.
- **Esforço:** P.
- **Suspeita.** Como confirmar: testar os downloads (DXF, STL, PNG, PDF) em Safari iOS/iPadOS real com arquivos de alguns MB, pelas telas de peça, configurações e versão.

#### M39 — Toasts de erro somem em 5 s, são anunciados como "polite" e a deduplicação não renova o tempo

- **Onde:** `frontend/src/app/shared/services/toast.service.ts:42`; `frontend/src/app/shared/components/toast-container/toast-container.component.ts:22-24`.
- **Trecho:** `if (this._toasts().some((toast) => toast.tipo === tipo && toast.mensagem === mensagem)) return;`
- **Problema:** todo toast some em 5 s fixos, sem pausa em hover ou foco (WCAG 2.2.1). Erros usam `role="status"` dentro de `aria-live="polite"` (região viva aninhada, sem prioridade). Mensagem repetida é descartada sem renovar o temporizador.
- **Cenário:** salvar duas vezes com o mesmo erro, o segundo clique 4,5 s depois: o aviso some 0,5 s depois. Como muitos erros deste relatório só aparecem em toast, o efeito se soma.
- **Correção:** erro com `role="alert"` e duração maior ou persistente; pausar em hover/foco; na duplicata, reiniciar o temporizador; remover o `aria-live` do contêiner ou os roles dos itens.
- **Esforço:** P.

#### M40 — `cw-campo` não liga a mensagem de erro ao controle e fica mudo para chaves sem tradução

- **Onde:** `frontend/src/app/shared/ui/campo.component.ts:79`.
- **Trecho:** `const chave = Object.keys(erros).find((k) => this.mensagens()[k]); return chave ? this.mensagens()[chave] : null;`
- **Problema:** se o controle tem um erro cuja chave não está em `mensagens`, `erro()` devolve `null`: sem mensagem e sem borda, mas o formulário continua inválido. A div de erro não tem id nem `aria-describedby`, e o controle não recebe `aria-invalid`.
- **Cenário:** campo com `required` e `min(0)` e só `required` mapeado: digitar -1 e salvar. Nada aparece; o botão parece não funcionar.
- **Correção:** fallback genérico ("Valor inválido.") com a classe `invalido` ligada a `invalid && touched`; ids `para() + '-erro'`/`'-dica'` expostos via `aria-describedby`, e `aria-invalid`.
- **Esforço:** M.

#### M41 — SCSS do `ferramenta-ia` a 60 bytes de quebrar o build de produção

- **Onde:** `frontend/angular.json:66`.
- **Trecho:** `{ "type": "anyComponentStyle", "maximumWarning": "4kB", "maximumError": "8kB" }`
- **Problema:** `src/app/shared/ui/ferramenta-ia/ferramenta-ia.component.scss` compila em 7,94 kB contra erro em 8 kB (confirmado pelo aviso do `npm run build`).
- **Cenário:** mais uma regra CSS de uns 60 bytes minificados e o build de produção falha.
- **Correção:** dividir o `ferramenta-ia` em subcomponentes com estilos próprios ou mover estilos compartilhados para o tema global. Subir o budget só como decisão consciente.
- **Esforço:** M.

#### M42 — Ferramentas e assistente sem testes: componentes, pontes, conversa e parser de SSE

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:44`; `frontend/src/app/data/assistente/ferramentas.service.spec.ts:13`.
- **Trecho:** `it('deve montar a rota pelo escopo: versão, peça do projeto ou projeto', () => { ... });`
- **Problema:** não há spec para `Modelo3dComponent`, `PonteSolidCraft`, `Desenho2dComponent`, `PonteCadCraft`, `HistoricoDesenhoComponent`, `ConversaIaComponent`, `BriefingComponent`, `BriefingService`, `FerramentaIaComponent`, `ArquivosDoProjetoComponent`, `AvisoPecaPublicadaComponent` e `EscolhaIaComponent`. O spec de `FerramentasService` só confere URLs: `acompanhar` (`ferramentas.service.ts:66-100`, parser incremental de SSE) e `getArquivo` (cabeçalhos e 204) não são testados. Testes com "sempre do servidor" no nome não verificam a `fetchPolicy`.
- **Cenário:** as regressões de A2, A4, M1, M3 e M17 passam pelo CI; uma mudança no corte de blocos do SSE (aceitar `\r\n`) quebra o andamento ao vivo com a suíte verde.
- **Correção:** por prioridade: (1) `acompanhar` com `HttpTestingController` emitindo `DownloadProgress` (evento partido entre dois trechos, dois eventos num trecho, `fim`, JSON inválido); (2) as duas pontes com iframe falso; (3) os dois componentes com a ponte injetável (fábrica via token): iniciar com erro, salvar durante a troca, duas trocas, restaurar em duplo clique, marcar final em peça publicada; (4) `ConversaIaComponent`: envio, erro HTTP, retomada com `processando: true`, destruição no meio; (5) briefing: 204, erro, um único recarregamento do PDF. Os testes temporários dos verificadores descritos neste relatório servem de ponto de partida.
- **Esforço:** G.

#### M43 — Projetos: 4 arquivos sem spec e nenhum teste de caminho de erro

- **Onde:** `frontend/src/app/modules/projetos/versao-detalhe/versao-detalhe.component.spec.ts:175`.
- **Trecho:** `expect(TestBed.inject(Router).url).toBe('/projetos/1/versoes/30');`
- **Problema:** sem spec: `novo-projeto.component.ts`, `arquivos-versao.component.ts` (559 linhas), `etapas-versao.component.ts`, `pecas-em-rascunho.component.ts`. Em toda a área não há teste com serviço falhando. Os specs montam o componente sem roteador real; o de Duplicar confere só a URL.
- **Cenário:** A1 e A5 chegaram à branch com a suíte verde.
- **Correção:** specs de erro por componente; Duplicar com `RouterTestingHarness`; `arquivos-versao` (carga, abas fora de ordem, falha, permissões); `novo-projeto` (validação, 400 no campo, navegação).
- **Esforço:** M.

#### M44 — Precificação, peças e ateliê: testes só do caminho feliz; módulo ateliê sem nenhum teste

- **Onde:** `frontend/src/app/modules/precificacao/precificacao.component.spec.ts:24`; `frontend/src/app/modules/atelie/atelie.component.ts:26`.
- **Trecho:** `getPrecos: vi.fn(() => of(precos)),`
- **Problema:** todos os mocks devolvem `of(...)` com o mesmo objeto, o que esconde A1 e M28. `formatos.ts` não tem spec (`diasDesde` com data inválida devolve `NaN`; a fronteira de 15/16 dias não é fixada). Não existem `atelie.component.spec.ts` nem `atelie.service.spec.ts`: a preservação de `endereco` no PUT (a tela reenvia o campo sem ter como editá-lo) não tem cobertura.
- **Cenário:** uma mudança que deixe de reenviar `endereco` apaga o endereço do ateliê no backend sem teste acusar; trocar `> 15` por `>= 15` também passa.
- **Correção:** `formatos.spec.ts` com as fronteiras; na página, `getPrecos` com `throwError` e releitura com objeto novo; em peças, erro, vazio, busca e falhas do visualizador; ateliê: carga, salvar com `endereco` preservado, logo inválido, 400 no campo, GET do logo com 204.
- **Esforço:** M.

#### M45 — Core: testes fixam os comportamentos errados e não cobrem as falhas de sessão

- **Onde:** `frontend/src/app/core/guards/guards.spec.ts:47`; `frontend/src/app/core/interceptors/jwt.interceptor.spec.ts:80-82`.
- **Trecho:** `it('deve redirecionar e limpar o storage quando o token venceu', async () => {`
- **Problema:** `guards.spec` fixa o M4; `jwt.interceptor.spec` só testa a falha do refresh com `Error` genérico e tem um `mockReturnValue` morto na linha 80; `auth.refresh.spec` não testa erro do refresh nem logout durante o refresh; `app.routes.spec` só usa token com todas as permissões; não há spec para `LogoutService`, `RefreshTokenService` nem `UsuarioComponent`.
- **Cenário:** corrigir A6 ou M4 quebra testes que descrevem o bug como esperado; uma regressão no caminho de falha do refresh passa.
- **Correção:** os casos listados em A6, M4, M5, B28 e B29; remover a linha morta e verificar os argumentos de `logout`.
- **Esforço:** M.

#### M46 — Shared: componentes com recurso assíncrono e serviços de infraestrutura sem teste

- **Onde:** `frontend/src/app/shared/ui/ui.spec.ts:17`.
- **Trecho:** `describe('Componentes de UI', () => { describe('CampoComponent', ...`
- **Problema:** sem spec: `DownloadService`, `ImagemProtegidaComponent`, `VisualizadorPdfComponent`, `PainelPdfComponent`, `AnexosImagemComponent` (só `ehHeic`), `ToastContainer`, `LoadingService`, `LocalStorageService`, `CampoBusca`. O teste de `QuantidadeComponent` usa um pai que sempre devolve o valor novo.
- **Cenário:** remover um `URL.revokeObjectURL` ou a guarda `destruido` passa em toda a suíte.
- **Correção:** separar `ui.spec.ts` por componente e cobrir: imagem protegida (sucesso, erro, troca de `src`, revogação), painel PDF, download, anexos (limite, concorrência), quantidade (falha e destroy).
- **Esforço:** M.

### Baixo

#### B1 — Parser de SSE: bloco que não é JSON válido faz reentregar eventos em dobro e nunca chegar ao fim

- **Onde:** `frontend/src/app/data/assistente/ferramentas.service.ts:71-82`.
- **Trecho:** `const evento = JSON.parse(dados) as EventoDoTurnoDto; ... entregar(parcial.slice(lido)); lido = parcial.length;`
- **Problema:** `JSON.parse` roda dentro do `next` sem try/catch. A exceção sai de `entregar()` antes de `lido = parcial.length`, mas `resto` já foi alterado: no próximo progresso os eventos anteriores são emitidos de novo e o laço volta a parar no bloco inválido. O assinante nunca recebe `error`.
- **Cenário:** um proxy insere uma página de erro no meio do stream: texto duplicado no chat a cada trecho e o `fim` não chega. Com o backend atual o gatilho depende de intermediário.
- **Correção:** atualizar `lido` antes de `entregar` (ou em `finally`) e try/catch por bloco. Cobrir com teste.
- **Esforço:** P.

#### B2 — `getArquivo` transforma cabeçalho `X-Arquivo-Id` ausente em id 0

- **Onde:** `frontend/src/app/data/assistente/ferramentas.service.ts:131`.
- **Trecho:** `return { id: Number(r.headers.get('X-Arquivo-Id')), conteudo: r.body, ... };`
- **Problema:** `Number(null)` é 0. A política de CORS do backend (`ApiExtensions.cs:23-26`) não tem `WithExposedHeaders`; hoje front e API são mesma origem.
- **Cenário:** API em outro domínio: `atualId = 0`, aviso "atual não é o final" permanente e ações indo para o arquivo 0.
- **Correção:** lançar erro se o cabeçalho faltar ou não for numérico; expor os dois cabeçalhos no CORS.
- **Esforço:** P.

#### B3 — Modo mock (`usarMocks`) quebrado, desatualizado e embarcado no bundle de produção

- **Onde:** `frontend/src/app/data/auth/login/login.service.ts:9-19`; `frontend/src/app/data/auth/login/mocks/usuarios-demo.mock.ts:13-36`.
- **Trecho:** `if (this.ambiente.usarMocks) return this.postLoginMock(dto);`
- **Problema:** só login, logout, refresh e o `DocumentoService` têm mock; o resto vai à API real com JWT falso. Os usuários demo não têm `projetos.*` nem `atelie.configurar`: até a "joalheira: acesso completo" cai em `/403` (M5). `usarMocks` é valor de runtime, então os mocks e a `SENHA_DEMO` vão no bundle de produção, inertes. O comentário de `environment.development.ts` promete trabalhar sem backend.
- **Cenário:** um dev liga `usarMocks: true` e entra com o usuário demo: "Acesso negado", sem menu.
- **Correção:** remover o modo mock da camada data (mantendo `token-payload.mock.ts` e `codificar-token.mock.ts` para testes) ou completá-lo; se ficar, trocar o desvio em runtime por substituição de provider no build de desenvolvimento.
- **Esforço:** M.

#### B4 — DTOs declaram não-nulo o que o schema GraphQL devolve como anulável

- **Onde:** `frontend/src/app/data/common/graphql/pagina.dto.ts:2`; `data/clientes/clientes/clientes.service.ts:74`; `data/projetos/projetos/dto/projeto.dto.ts:56-63`; `data/atelie/atelie.service.ts:61`.
- **Trecho:** `readonly nodes: readonly T[];`
- **Problema:** as connections e `nodes` são anuláveis no schema; `PrecosDoProjeto.cotacoes` e `parametros` também (sob `@authorize`). `getLogo` declara `Observable<Blob>` para uma rota que responde 204 e só não quebra porque o `withFetch()` entrega Blob vazio.
- **Cenário:** resolver de listagem falha: `getClientesOpcoes` executa `dados.clientes.nodes` e lança `TypeError`.
- **Correção:** refletir o schema nos tipos internos e normalizar no serviço (junto do M7). Vale gerar os tipos com graphql-codegen a partir de `backend/test/IntegrationTests/GraphQl/schema.graphql`.
- **Esforço:** M.

#### B5 — Apollo: `fetchPolicy` inconsistente, `typePolicies` de exemplo e cache que só cresce e não é limpo no logout

- **Onde:** `frontend/src/app/data/projetos/projetos/projetos.service.ts:321`; `frontend/src/app/core/factories/apollo.factory.ts:26`.
- **Trecho:** `existente ?? (typeof args?.['id'] === 'string' ? toReference({ __typename: 'Peca', id: args['id'] }) : undefined)`
- **Problema:** o padrão global já é `network-only`; 11 consultas o repetem e 6 não dizem nada. Não há `watchQuery`, `readQuery` nem `refetch`: o `InMemoryCache` só recebe escrita. `Peca.etapas` e `ComposicaoPreco` não existem no schema; o redirect de `Query.peca` testa `typeof id === 'string'`, mas os serviços mandam número. O comentário de `OPCOES_PADRAO` diverge do código (`watchQuery` está `cache-and-network`). `limparSessao` não chama `clearStore()`.
- **Cenário:** alguém muda o padrão para `cache-first`: as 6 consultas sem política passam a mostrar dados antigos após salvar (as escritas são REST).
- **Correção:** uma regra só (confiar no padrão com um teste que o fixe, ou declarar em todas); avaliar `no-cache` como padrão; remover as `typePolicies` sem correspondência; corrigir o comentário; `apollo.client.clearStore()` no logout.
- **Esforço:** P.

#### B6 — `projetos.service.ts` com 505 linhas e quatro assuntos; README de `data` descreve estrutura que nenhum serviço segue

- **Onde:** `frontend/src/app/data/projetos/projetos/projetos.service.ts:15`; `frontend/src/app/data/README.md`.
- **Trecho:** `export const GET_PROJETO = gql\`...\`` (8 documentos, linhas 15-271) e 40 métodos.
- **Problema:** o arquivo junta projeto, peças do projeto, preços do projeto e orçamento da versão, com 270 linhas de gql inline. O README manda separar em `queries/` e nomear DTOs por verbo; nada segue isso. `map((r) => (r.status === 204 || !r.body ? null : r.body))` aparece 8 vezes e `.pipe(map(() => undefined))` mais de 25.
- **Cenário:** quem segue o README cria um segundo padrão na mesma área.
- **Correção:** dividir em `ProjetosService`, `PecasDoProjetoService` e `PrecoDaVersaoService`/`PrecosDoProjetoService`; mover os documentos para `queries/` ou atualizar o README para o padrão real; extrair `blobOuNulo()` e `semCorpo()` em `data/common`.
- **Esforço:** M.

#### B7 — Código morto e placeholders no menu

- **Onde:** `frontend/src/app/data/documentos/documento/documento.service.ts:19`; `data/common/http/criar-http-params.ts`; `shared/services/download.service.ts:20` (`abrir`); `shared/ui/quantidade.component.ts:102` (`valorAtual`); `shared/injection-tokens/http-context.tokens.ts:12` (`semUrlBase`); `ToastService.info`/`aviso`; `PermissaoService.temTodas`; `app.routes.ts:98`, `:109`.
- **Trecho:** `.get(\`documentos/pecas/${encodeURIComponent(pecaId)}/${tipo}.pdf\`, { responseType: 'blob' })`
- **Problema:** `DocumentoService` (com `tipo-documento.ts` e `pdf.mock.ts`) chama uma rota que não existe no backend e só é referenciado pelo próprio spec; por isso o knip não acusa. Os demais itens não têm chamador em produção. `data/clientes/.gitkeep`, `shared/directives/`, `shared/pipes/`, `core/validators/` sobraram vazios. Documentos e Portfólio são páginas só com `<h1>` e estão no menu.
- **Cenário:** alguém liga um botão "Orçamento em PDF" a `getDocumentoPdf` confiando no spec verde: 404 em produção e, por ser Blob, sem toast. Usuário com `portfolio.ler` clica no menu e cai numa página só com o título.
- **Correção:** remover o serviço e companhia, os membros mortos e as pastas vazias (ou marcar como contrato futuro); tirar Documentos e Portfólio do menu ou mostrar "em breve".
- **Esforço:** P.

#### B8 — READMEs desatualizados, regra de módulos furada por import relativo e domínio fora do lugar

- **Onde:** `frontend/src/app/shared/README.md:5`; `frontend/src/app/modules/ferramentas/briefing/briefing.component.ts:19`; `frontend/src/app/data/assistente/dto/ferramenta.dto.ts:1`; `frontend/src/app/core/README.md`.
- **Trecho:** `import { ConversaIaComponent } from '../../assistente/conversa-ia.component';`
- **Problema:** o README de shared lista modal, offcanvas, CVAs e serviços que não existem e omite `ui/`, `forms/`, `arquivos/` e `pecas/`; o de core anuncia `GlobalErrorHandler` e não cita o `convidadoGuard`. `shared/pecas/tipo-peca.ts` e `shared/ui/ferramenta-ia` contrariam "nada aqui conhece um domínio". O `modules/README.md` diz que um módulo nunca importa outro, mas o ESLint só casa o alias `@modules/*`: briefing, desenho-2d e modelo-3d importam `assistente` e `projetos` por caminho relativo. `PapelMensagem` mora no DTO do briefing e só `data/assistente` usa. Comentários errados: `ferramentas.service.ts:197` (JSDoc órfão), `briefing.service.ts:24`, `briefing.component.ts:156`, `post-login-resposta.dto.ts:4`, comentários de "Etapa 3/4".
- **Cenário:** mover `conversa-ia.component.ts` ou `tipo-joia.ts` quebra três ferramentas sem o lint avisar da dependência.
- **Correção:** reescrever os READMEs com a estrutura real; ampliar `no-restricted-imports` para caminhos relativos entre módulos (ou eslint-plugin-boundaries); mover `PapelMensagem` para `data/assistente/dto`; corrigir os comentários. Reorganização na seção 3.
- **Esforço:** M.

#### B9 — Modelo 3D: erros sem tratamento ou com toast duplicado em salvar, conferência, histórico e baixar

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:206`, `:157`, `:213`.
- **Trecho:** `this.historico.set(await firstValueFrom(this.ferramentas.getHistorico(this.projetoId, this.escopo(), FERRAMENTA)));`
- **Problema:** `abrirHistorico` e `baixar` não têm try/catch; no `baixar` a falha é silenciosa (Blob) e a versão em branco (204) também não avisa. `salvar` e `abrirConferencia` mostram o próprio toast além do do interceptor.
- **Cenário:** baixar uma versão com 500: nada acontece. Salvar com o servidor fora: dois toasts.
- **Correção:** um helper único para as operações (try/catch/finally, filtra `HttpErrorResponse` não-Blob, toast próprio para Blob), aplicado também ao 2D.
- **Esforço:** P.

#### B10 — Ferramentas não reagem à troca de `id`/`versaoId`/`pecaId` com a instância reaproveitada

- **Onde:** `frontend/src/app/modules/ferramentas/modelo-3d/modelo-3d.component.ts:123`; rotas em `frontend/src/app/app.routes.ts:28`.
- **Trecho:** `constructor() { afterNextRender(() => void this.iniciar()); }`
- **Problema:** o arquivo é aberto uma única vez. Se o router reutilizar a instância com outros parâmetros, `escopo()` muda e a conversa recarrega, mas o editor continua com o modelo anterior; o próximo Salvar grava esse modelo no escopo novo. O reuso foi provado em teste (verificação do M2: mesma instância e `carregar()` chamado 1 vez). Navegar entre as rotas irmãs versão/peça sempre recria. Também `/projetos/1/pecas/nova/modelo-3d` casa com `pecaId='nova'`.
- **Cenário:** Voltar/Avançar ou edição da URL de `/versoes/2/modelo-3d` para `/versoes/3/modelo-3d`: modelo da 2 com a conversa da 3. Nenhum link da interface faz essa navegação hoje.
- **Correção:** `effect` que recarrega quando `projetoId`/`escopo` mudarem (reaproveitando a fila do A2), ou forçar a recriação nessas rotas. Agrupar as quatro rotas de ferramenta sob um pai com os guards.
- **Esforço:** P (M com o agrupamento).
- **Nota de fusão:** a área de core classificou este ponto como Médio e suspeita; ficou Baixo porque o verificador do M2 confirmou o mecanismo e o avaliou como baixo risco, por não haver caminho na interface.

#### B11 — Chat: "Desfazer" em todos os cartões e ações sem estado de ocupado nem tratamento de erro

- **Onde:** `frontend/src/app/shared/ui/ferramenta-ia/ferramenta-ia.component.ts:193`; `frontend/src/app/modules/assistente/conversa-ia.component.ts:160-185`.
- **Trecho:** `@if (!acao.desfeita) { <button type="button" class="btn btn-sutil" (click)="desfazer.emit(m.id)">Desfazer</button> }`
- **Problema:** o domínio só aceita desfazer a ação mais recente ainda valendo e recusa durante o turno; o botão aparece em todos os cartões, sem `[disabled]`. `novoChat`, conversas anteriores, `abrirChat`, `desfazer` e `apagar` são `async` sem try/catch e sem sinal de "em andamento".
- **Cenário:** três ações aplicadas, Desfazer na primeira: 422. Toque duplo em Desfazer: o segundo POST dá erro logo após o sucesso do primeiro (e alimenta a corrida do A2).
- **Correção:** mostrar o botão só no último cartão com ação não desfeita; signal `ocupado` no `ConversaIaComponent` em try/finally, repassado ao `cw-ferramenta-ia` junto com `pensando`.
- **Esforço:** P.

#### B12 — Ids duplicados: `titulo-historico` no diálogo de conversas e no histórico do arquivo

- **Onde:** `frontend/src/app/modules/ferramentas/desenho-2d/historico-desenho.component.ts:24` (igual no 3D); `shared/ui/ferramenta-ia/ferramenta-ia.component.ts:276`.
- **Trecho:** `<dialog #dialogo class="historico" aria-labelledby="titulo-historico" ...>`
- **Problema:** dois elementos com o mesmo id; `aria-labelledby` resolve para o primeiro.
- **Cenário:** leitor de tela anuncia o Histórico do desenho como "Conversas anteriores".
- **Correção:** ids distintos ou gerados por instância.
- **Esforço:** P.

#### B13 — Textos fixos de "desenho" no componente compartilhado aparecem no Modelo 3D e no Briefing

- **Onde:** `frontend/src/app/shared/ui/ferramenta-ia/ferramenta-ia.component.ts:146`; `andamento-ia.component.ts:45`, `:75`.
- **Trecho:** `placeholder="Peça uma mudança no desenho..."`
- **Problema:** `cw-ferramenta-ia` serve às três ferramentas com textos fixos de desenho.
- **Cenário:** no Briefing o campo pede "uma mudança no desenho" e a confirmação diz que "o desenho" não muda.
- **Correção:** textos por input (objeto `rotulos` por ferramenta, com padrão neutro).
- **Esforço:** P.

#### B14 — Largura salva do assistente não é limitada à janela atual ao carregar

- **Onde:** `frontend/src/app/shared/ui/ferramenta-ia/ferramenta-ia.component.ts:492`.
- **Trecho:** `return salva >= LARGURA_MINIMA ? salva : LARGURA_PADRAO;`
- **Problema:** o teto de 70% da janela só vale no arrasto; o valor lido do localStorage entra direto em `--largura-chat`, e não há reação a resize.
- **Cenário:** assistente arrastado a 1340 px numa janela de 1920; ao abrir com 1000 px a área de trabalho fica com largura 0 e o divisor fora da tela.
- **Correção:** o mesmo clamp na leitura e no resize, ou no CSS: `min(var(--largura-chat), 70vw)`.
- **Esforço:** P.

#### B15 — CADCraft é carregado duas vezes na abertura do Desenho 2D

- **Onde:** `frontend/src/app/modules/ferramentas/desenho-2d/desenho-2d.component.html:45`; `ponte-cadcraft.ts:100`.
- **Trecho:** `<iframe #cadcraftA src="/cadcraft/" ... (load)="iniciar()"></iframe>`
- **Problema:** o iframe nasce em `/cadcraft/` e o `(load)` leva a `recarregar()`, que faz `location.reload()`: dois boots do WebAssembly. Todo `load` do iframe A (inclusive o `about:blank` do descarregar) chama `iniciar()` de novo.
- **Cenário:** no tablet, o tempo até o desenho ficar disponível é cerca do dobro.
- **Correção:** começar em `about:blank` e iniciar por `afterNextRender`, como no 3D.
- **Esforço:** P.

#### B16 — Rolagem forçada para o fim a cada evento do turno impede reler mensagens

- **Onde:** `frontend/src/app/shared/ui/ferramenta-ia/ferramenta-ia.component.ts:365`.
- **Trecho:** `afterRenderEffect(() => { this.mensagens(); this.andamento(); this.pensando(); this.fim().nativeElement.scrollIntoView({ block: 'end' }); });`
- **Problema:** o efeito depende de `andamento()`, que muda a cada trecho de SSE.
- **Cenário:** durante um turno longo, rolar para cima: a lista salta de volta várias vezes por segundo.
- **Correção:** só rolar se a lista já estava perto do fim, ou só quando `mensagens()`/`pensando()` mudarem.
- **Esforço:** P.

#### B17 — Depois de salvar a peça, o "Peso: … · origem" fica desatualizado

- **Onde:** `frontend/src/app/modules/projetos/peca-do-projeto/peca-do-projeto.component.ts:231`.
- **Trecho:** `this.toast.sucesso('Peça alterada.'); this.peca.reload();`
- **Problema:** o peso que vale vem do recurso `peso` (linha 143), que não é relido ao salvar; `finais` também não.
- **Cenário:** alterar "Peso (g)" de 5 para 7 e salvar: a linha "Peso: 5,000 g · digitado" continua até recarregar.
- **Correção:** `this.peso.reload()` e `this.finais.reload()` junto do `peca.reload()`.
- **Esforço:** P.

#### B18 — Recargas trocam conteúdo por esqueleto ou "Calculando…": params com objeto novo e `isLoading()` sem `hasValue()`

- **Onde:** `frontend/src/app/modules/projetos/versao-detalhe/preco-versao.component.ts:101`; `versao-detalhe.component.ts:100`; `peca-do-projeto/peca-do-projeto.component.html:4`.
- **Trecho:** `params: () => ({ projetoId: this.projetoId(), versao: this.versao(), comCalculo: this.podeVerCalculo() }),`
- **Problema:** o objeto `versao` inteiro entra em `params`: a cada `projeto.reload()` o resource descarta o valor e o card recria o DOM do formulário aberto. Em `pesos`, mudar só a quantidade refaz a medição. Na peça, `isLoading()` também é verdadeiro em `reloading`.
- **Cenário:** formulário "Metais e peso" aberto; alterar uma quantidade: o card pisca para "Calculando o preço..." e o campo perde o foco.
- **Correção:** `params` só com primitivos e `preco.reload()` quando a composição mudar; chave por `computed` com igualdade em `pesos`; `[carregando]="editando() && !peca.hasValue() && peca.isLoading()"`.
- **Esforço:** P.

#### B19 — "Definir valor" na lista de peças apaga o valor existente para quem tem `precificacao.editar` sem `precificacao.ler`

- **Onde:** `frontend/src/app/modules/projetos/projeto-detalhe/pecas-projeto.component.ts:201`.
- **Trecho:** `this.valor.reset(peca.valor ?? null);`
- **Problema:** o botão depende só de `podeEditarValor`, mas o valor atual só vem com `precificacao.ler`. Sem ela o campo abre vazio e "Salvar" envia `{ valor: null }`, que tira o valor.
- **Cenário:** "Definir valor" numa peça que vale R$ 120 e "Salvar" sem digitar: "Valor retirado." e a versão ganha a pendência PECA_SEM_VALOR.
- **Correção:** exibir o botão só com `podeEditarValor() && podeVerValor()`. Antes, verificar se o catálogo de perfis permite essa combinação.
- **Esforço:** P.

#### B20 — Commands assinados sem callback de erro: exceção não tratada e estado sem releitura

- **Onde:** `frontend/src/app/modules/projetos/versao-detalhe/versao-detalhe.component.ts:183`, `:197`; `projeto-detalhe.component.ts:84`; `projeto-configuracoes.component.ts:86`, `:104`; `precos-projeto.component.ts:304`; `preco-versao.component.ts:343`.
- **Trecho:** `.pipe(finalize(() => this.salvando.set(false))).subscribe(() => { ... });`
- **Problema:** o interceptor mostra o toast e relança; sem `error`, cada 400/403/409/422 vira exceção não tratada. Em `arquivar`, a tela também não relê o projeto.
- **Cenário:** outro usuário já arquivou a versão; "Arquivar": 422 e a tela continua mostrando "Em desenvolvimento" com os botões de edição.
- **Correção:** usar o mesmo `executar` (com `error: () => this.projeto.reload()`), idealmente um helper único de command.
- **Esforço:** P.

#### B21 — Seletor de cliente lista só os 50 primeiros, sem busca

- **Onde:** `frontend/src/app/modules/projetos/novo-projeto/novo-projeto.component.html:10`; `frontend/src/app/data/clientes/clientes/clientes.service.ts:37`.
- **Trecho:** `clientes(termo: $termo, first: 50, where: { ativo: { eq: true } })`
- **Problema:** `getClientesOpcoes()` aceita `termo`, mas as telas chamam sem termo e usam `<select>`.
- **Cenário:** ateliê com 60 clientes ativos: o 51º não pode ser escolhido por nenhum caminho.
- **Correção:** campo com busca (autocomplete com debounce) ou, no mínimo, aviso de lista truncada.
- **Esforço:** M.

#### B22 — Id de rota não numérico vira `NaN` e mostra "erro de carga" em vez de "não encontrado"

- **Onde:** `frontend/src/app/modules/projetos/projeto-detalhe/projeto-detalhe.component.ts:54`; `modules/pecas/peca-detalhe/peca-detalhe.component.ts:69`; mesmo padrão em projeto-configuracoes (41, 46), versao-detalhe (79, 86, 91, 101), peca-do-projeto (135, 144, 150), desenho-da-peca (259-311) e briefing.
- **Trecho:** `params: () => Number(this.id()),`
- **Problema:** `Number('abc')` é `NaN`, que vira `null` para um `Long!` e `projetos/NaN/briefing` no REST. O estado "não encontrado" só cobre id numérico inexistente.
- **Cenário:** abrir `/projetos/abc` ou `/pecas/12x`: toasts técnicos e "Tentar de novo" que nunca vai funcionar.
- **Correção:** helper `idDaRota(input)` (`Number.isSafeInteger(n) && n > 0`, senão não consultar e cair em "não encontrado"), ou matcher de rota numérica.
- **Esforço:** P.

#### B23 — Etapas da versão é um protótipo em localStorage exposto como funcionalidade

- **Onde:** `frontend/src/app/modules/projetos/versao-detalhe/etapas-versao.component.ts:213`.
- **Trecho:** `salvas = JSON.parse(localStorage.getItem(chave) ?? 'null') as Etapa[] | null;`
- **Problema:** o estado vive só no navegador e a chave usa só o `versaoId`: cada dispositivo vê um progresso diferente e duas contas no mesmo navegador compartilham o estado. O `JSON.parse` é só convertido por cast: um valor que não seja array quebra o template.
- **Cenário:** "Fundição" concluída no tablet da bancada aparece como 0 de 7 no computador do atendimento.
- **Correção:** sinalizar que é local (ou esconder atrás de flag), validar com `Array.isArray`, incluir ateliê/usuário na chave e planejar a persistência no servidor.
- **Esforço:** M.

#### B24 — Duplicação no módulo de projetos

- **Onde:** `executar` em `frontend/src/app/modules/projetos/projeto-detalhe/projeto-detalhe.component.ts:120`, `pecas-projeto.component.ts:215`, `versao-detalhe/versao-detalhe.component.ts:211` (e `gravar` em `preco-versao.component.ts:341`); `moeda`/`numero` em `precos-projeto.component.ts:24-31` e `preco-versao.component.ts:31-38`; `peca-do-projeto.component.ts:139`.
- **Trecho:** `executar(comando, sucesso)` repetido em três componentes.
- **Problema:** helper de command, formatadores, formulário "definir valor" (pecas-projeto e peca-do-projeto), título "Briefing de …" e o bloco de download manual (ver M38) estão copiados. A página da peça busca todas as peças do projeto e filtra no cliente, embora a API aceite `pecas(id:)`. `tipo-joia.ts` mora em `modules/projetos` e só o briefing usa. `arquivos-versao` (559 linhas, template e estilo inline) e `preco-versao` (349 ts + 455 html) são candidatos a subcomponentes.
- **Cenário:** uma correção feita em uma cópia de `executar` (tratar erro, B20) não chega às outras.
- **Correção:** helper `executarComando({ comando, sucesso, recarregar })`, formatadores em shared, `cw-valor-da-peca`, consulta `getPecaDoProjeto(id, pecaId)` e `tipo-joia.ts` no módulo do briefing.
- **Esforço:** M.

#### B25 — Visualizador de desenho: carga única no construtor e blob URLs criadas depois do destroy

- **Onde:** `frontend/src/app/modules/pecas/desenho-da-peca/desenho-da-peca.component.ts:161`.
- **Trecho:** `queueMicrotask(() => { void this.abrir(); void this.carregarNome(); });`
- **Problema:** `id`, `ferramenta` e `pecaId` são lidos uma única vez num microtask; com o componente reaproveitado, a tela mantém o desenho antigo. `abrir`, `mostrar` e `abrirPrevia` continuam depois dos `await` com o componente destruído e criam object URLs que ninguém revoga.
- **Cenário:** abrir o desenho e voltar antes de a imagem chegar: um Blob preso na memória por ocorrência.
- **Correção:** `rxResource` com `params: () => ({ id, ferramenta, pecaId })`; enquanto isso, flag `destruido` checada após cada `await`.
- **Esforço:** M.

#### B26 — Ateliê: falha ao buscar o logo é silenciosa e a tela passa a dizer que não há logo

- **Onde:** `frontend/src/app/modules/atelie/atelie.component.ts:156`.
- **Trecho:** `this.servico.getLogo().subscribe((blob) => this.trocarLogoUrl(blob.size > 0 ? URL.createObjectURL(blob) : null));`
- **Problema:** sem callback de erro nem `takeUntilDestroyed`; por ser Blob, sem toast (M6). O tipo `Observable<Blob>` não reflete o 204 (B4). "Remover" apaga com um clique.
- **Cenário:** `possuiLogo` verdadeiro e GET em 500: a moldura mostra a inicial e o botão vira "Enviar logo".
- **Correção:** `Observable<Blob | null>` com `observe: 'response'`, estado de erro próprio, `takeUntilDestroyed` e confirmação no "Remover".
- **Esforço:** P.

#### B27 — Ateliê: "Desfazer" recarrega, mas o formulário continua sujo e some por um instante

- **Onde:** `frontend/src/app/modules/atelie/atelie.component.ts:66`; `atelie.component.html:81`.
- **Trecho:** `this.form.setValue({ nome: c.nome, assinatura: c.assinatura ?? '', ... });`
- **Problema:** `setValue` não devolve o formulário a pristine; `carregar()` destrói o `<form>` e mostra o esqueleto durante a requisição.
- **Cenário:** alterar o nome e clicar em "Desfazer": a tela pisca e o botão continua habilitado.
- **Correção:** guardar a última configuração e fazer `form.reset(valoresSalvos)` local; limpar `errosGerais`.
- **Esforço:** P.

#### B28 — Logout repetido por requisição falha pode sobrescrever o `returnTo` com a própria URL do login

- **Onde:** `frontend/src/app/core/interceptors/jwt.interceptor.ts:51`; `frontend/src/app/core/services/auth.service.ts:81`.
- **Trecho:** `auth.logout(router.url);`
- **Problema:** não existe estado de "logout em andamento". Uma requisição que recebe 401 depois da navegação para `/login` chama `logout('/login?returnTo=…')`, e o `retornoSeguro` devolve `/` para o que começa com `/login`.
- **Cenário:** uma requisição rápida e uma lenta com o token vencido: após logar, o usuário cai em `/` em vez da versão.
- **Correção:** `logout()` idempotente e, no interceptor, não passar `returnTo` quando `router.url` começar com `/login`.
- **Esforço:** P.

#### B29 — Logout com refresh em andamento regrava os tokens depois do "Sair"

- **Onde:** `frontend/src/app/core/services/auth.service.ts:60`.
- **Trecho:** `tap((resposta) => this.gravarTokens(resposta.accessToken, resposta.refreshToken)),`
- **Problema:** `logout()` não invalida o `refreshEmAndamento$` (`shareReplay` com `refCount: false`). Se a resposta chegar depois de `limparSessao()`, o `tap` regrava os tokens.
- **Cenário:** tablet compartilhado: refresh em segundo plano e "Sair" no mesmo instante; a próxima pessoa entra direto na conta anterior enquanto o access valer.
- **Correção:** marcar a sessão como encerrada (contador de época ou `takeUntil`) para o `tap` não gravar depois do logout, e zerar `refreshEmAndamento$`.
- **Esforço:** P.

#### B30 — `UsuarioComponent`: permissão calculada uma vez, id duplicado e `role="dialog"` sem gestão de foco

- **Onde:** `frontend/src/app/layout/content-layout/usuario/usuario.component.ts:28`; `usuario.component.html:15`.
- **Trecho:** `protected readonly podeConfigurar = inject(PermissaoService).temAlguma(['atelie.configurar']);`
- **Problema:** `podeConfigurar` é um boolean do construtor (o menu ao lado usa `computed`); o mesmo padrão de `podeEditar` fixo se repete em várias telas. O layout instancia `cw-usuario` duas vezes com o mesmo `id="painel-usuario"`. O painel declara `role="dialog"` sem mover o foco.
- **Cenário:** o admin retira `atelie.configurar`; após o refresh o menu se ajusta, mas o painel continua mostrando "Ateliê e marca", que leva ao `/403`.
- **Correção:** `computed`, id por instância e `role="menu"` ou gestão de foco.
- **Esforço:** P.

#### B31 — `cw-quantidade`: campo digitado fica com texto diferente do valor quando o limite não altera o signal

- **Onde:** `frontend/src/app/shared/ui/quantidade.component.ts:92`.
- **Trecho:** `const limitado = Math.min(this.maximo(), Math.max(this.minimo(), valor)); this.atual.set(limitado);`
- **Problema:** se o valor limitado é igual ao que `atual` já tinha, o binding `[value]` não é reescrito.
- **Cenário:** quantidade 1 (mínimo 1): digitar 0 e sair; o campo mostra 0 e o modelo é 1.
- **Correção:** reescrever `alvo.value = String(limitado)` após limitar.
- **Esforço:** P.

#### B32 — `cw-imagem-protegida` vaza a object URL anterior quando `src` muda

- **Onde:** `frontend/src/app/shared/ui/imagem-protegida.component.ts:108`.
- **Trecho:** `if (url?.startsWith('blob:') && !this.src().startsWith('blob:')) URL.revokeObjectURL(url);`
- **Problema:** a revogação só acontece no destroy e só para a última URL. Hoje o único uso recria o componente a cada `src` (`@for … track src`), então o vazamento ainda não se manifesta.
- **Cenário:** reutilizar o componente com `[src]` variável: um Blob preso por troca.
- **Correção:** revogar no `onCleanup` do effect e zerar `url` ao iniciar nova carga.
- **Esforço:** P.

#### B33 — `aplicarErrosDoServidor` não resolve chaves de coleção e aceita lista de mensagens vazia

- **Onde:** `frontend/src/app/shared/forms/aplicar-erros-do-servidor.ts:14`.
- **Trecho:** `const controle = form.get(campo); if (controle) { controle.setErrors({ servidor: mensagens[0] });`
- **Problema:** o backend devolve chaves como `metais[0].ligaId`; `form.get` só quebra por ponto e devolve `null`. Com `mensagens` vazio, o controle fica inválido sem mensagem. Vários chamadores descartam o retorno `semCampo` (precos-projeto:298, ligas-atelie:194, cotacao-metal:263, parametros-preco:132).
- **Cenário:** 400 com erro em `metais[1].pesoGramas`: o erro do metal aparece, se for o primeiro, só no toast de 5 s.
- **Correção:** normalizar a chave (`campo.replace(/\[(\d+)\]/g, '.$1')`), tratar lista vazia e exibir `semCampo` nos chamadores.
- **Esforço:** P.

#### B34 — `cw-painel-pdf`: id fixo no título; lightbox não fecha com toque fora

- **Onde:** `frontend/src/app/shared/ui/painel-pdf.component.ts:13`; `imagem-protegida.component.ts:19`.
- **Trecho:** `<dialog #dialogo class="painel-pdf" aria-labelledby="titulo-pdf" (close)="fechado.emit()">`
- **Problema:** id fixo (duas instâncias duplicam). O comentário promete fechar por "toque fora", mas não há tratamento de clique no backdrop; o mesmo no `<dialog>` da imagem ampliada.
- **Cenário:** imagem ampliada no chat: tocar fora da foto não fecha.
- **Correção:** id por instância; no lightbox, fechar quando `event.target === dialog`; corrigir o comentário.
- **Esforço:** P.

#### B35 — `cw-estado-lista`: esqueleto sem semântica acessível e texto de erro fixo de conexão

- **Onde:** `frontend/src/app/shared/ui/estado-lista.component.ts:15`.
- **Trecho:** `<p>Verifique a conexão e tente de novo.</p>`
- **Problema:** `aria-label` numa div sem role não é anunciado. A segunda linha manda verificar a conexão para qualquer falha, inclusive 403 e 500.
- **Cenário:** sem permissão: o toast diz "Você não tem permissão" e o painel diz "Verifique a conexão".
- **Correção:** `role="status"` com texto oculto; tornar a segunda linha um input (`detalheErro`) ou removê-la.
- **Esforço:** P.

#### B36 — `PaginacaoCursor.proxima` aceita o mesmo cursor duas vezes

- **Onde:** `frontend/src/app/shared/components/paginacao/paginacao-cursor.ts:15`.
- **Trecho:** `if (!endCursor) return; this.historico.update((h) => [...h, this._atual()]); this._atual.set(endCursor);`
- **Problema:** sem guarda contra o cursor que já é o atual: o histórico ganha uma entrada repetida e o número da página avança.
- **Cenário:** dois acionamentos de "Próxima" no mesmo frame. Nos chamadores atuais o valor do resource é zerado na troca de parâmetros, o que estreita a janela.
- **Correção:** `if (!endCursor || endCursor === this._atual()) return;` e teste.
- **Esforço:** P.
- **Suspeita.** Como confirmar: teste unitário chamando `proxima('c1')` duas vezes e conferindo `numero()`.

#### B37 — Novo cliente: promete "completar depois" sem existir edição; CPF/CNPJ sem validação

- **Onde:** `frontend/src/app/modules/clientes/novo-cliente/novo-cliente.component.ts:24`; `novo-cliente.component.html:1`.
- **Trecho:** `subtitulo="Só o nome é obrigatório. O resto pode ser completado depois."`
- **Problema:** não há tela de detalhe/edição de cliente e o backend só tem `POST api/clientes`. O comentário diz que o documento valida "o tamanho", mas não há validador. `required` aceita nome só com espaços.
- **Cenário:** cadastrar "Ana" só com o nome e depois querer incluir o telefone: não existe caminho.
- **Correção:** trocar o subtítulo enquanto a edição não existir; planejar `PUT clientes/{id}`; `maxLength` no documento e trim no nome.
- **Esforço:** P.

#### B38 — Bundle inicial a 24 kB do aviso de budget

- **Onde:** `frontend/angular.json:62`.
- **Trecho:** `{ "type": "initial", "maximumWarning": "750kB", "maximumError": "1.5MB" }`
- **Problema:** Initial total de 726,00 kB; o CSS global (217,08 kB) responde por 30%.
- **Cenário:** uma dependência eager no shell soma mais de 24 kB e o build passa a avisar.
- **Correção:** acompanhar a cada PR; imports parciais do Bootstrap e do bootstrap-icons.
- **Esforço:** M.

#### B39 — knip fora das `devDependencies`, sem script npm e com a regra de tipos desligada

- **Onde:** `frontend/package.json:36`; `frontend/knip.json:4`.
- **Trecho:** `"rules": { "types": "off" }`
- **Problema:** o pacote não está instalado nem declarado; o npx baixou a 6.45.1 enquanto o `$schema` aponta para knip@5. A checagem não é reprodutível. Com `types` desligada, o resultado limpo não cobre tipos exportados sem uso; o knip também não aponta membros de classe (B7).
- **Cenário:** dois desenvolvedores rodam `npx knip` em datas diferentes e obtêm resultados diferentes; sem rede, falha.
- **Correção:** adicionar knip com versão fixa, script `"knip": "knip"` e alinhar o `$schema`; medir o volume com a regra de tipos ligada.
- **Esforço:** P.
- **Suspeita** quanto à existência de tipos mortos. Como confirmar: em `frontend/`, rodar `npx knip --include types`.

#### B40 — Chunk lazy `heic-to` de 3,23 MB

- **Onde:** `frontend/package.json:29`.
- **Trecho:** `"heic-to": "1.6.7"`
- **Problema:** maior chunk do build (525,85 kB de transferência estimada). É lazy; não foi verificado em que ponto o import dinâmico é disparado.
- **Cenário:** se o import ocorrer ao abrir a tela de anexos, e não só ao escolher um `.heic`, todo usuário baixa cerca de 526 kB à toa.
- **Correção:** garantir que o `import('heic-to')` só ocorra para HEIC/HEIF.
- **Esforço:** P.
- **Suspeita.** Como confirmar: conferir a condição antes do import em `shared/ui/anexos-imagem/preparar-imagem.ts`; na aba Network, anexar um JPEG e ver se o chunk é baixado.

## 3. Problemas estruturais

### 3.1 Duplicação

- **Desenho 2D × Modelo 3D (M15).** É a duplicação mais cara do projeto: cerca de 200 linhas de orquestração, os dois históricos, os dois `.scss` e os blocos de ações dos `.html`. Todo bug de corrida existe nos dois lados (A2, M1, M10) e três bugs existem só num lado porque a cópia divergiu (A4, M10, M1 no ⌘S do 2D).
- **Módulo de projetos (B24).** `executar(comando, sucesso)` em três componentes mais `gravar`; `moeda`/`numero` em dois; formulário "definir valor" em dois; download manual em dois apesar do `DownloadService` (M38); título do PDF do briefing em dois.
- **Camada data (B6).** "204 ou sem corpo → null" 8 vezes e `.pipe(map(() => undefined))` mais de 25; `fetchPolicy: 'network-only'` repetida em 11 consultas (B5).
- **Padrões repetidos que viraram defeito transversal.** `@let x = recurso.value()` (A1); `Number(this.id())` sem validação (B22); `podeEditar` calculado uma vez no construtor (B30); downloads em Blob sem tratamento (M6); `nomeDaFerramenta` em peca-detalhe duplicando o mapa `FERRAMENTAS` do desenho-da-peca.

### 3.2 Organização

- **Fronteira entre módulos furada (B8).** A regra do `modules/README.md` só é imposta para o alias `@modules/*`; os imports relativos passam. `ConversaIaComponent` (em `modules/assistente`) é usado por três ferramentas; `tipo-joia.ts` (em `modules/projetos`) só pelo briefing; `DesenhoDaPecaComponent` (em `modules/pecas`) serve também à rota de projetos.
- **`shared` conhece domínio e tem duas pastas de componentes.** `shared/pecas/tipo-peca.ts` e `shared/ui/ferramenta-ia` são domínio; `shared/components` e `shared/ui` coexistem sem critério (a classe `PaginacaoCursor` fica numa e o componente `cw-paginacao` na outra).
- **`data` não segue o próprio README (B6).** Sem `queries/`, DTOs agrupados por assunto, `PapelMensagem` no domínio errado, interceptors descritos em `data/common` quando estão em `core`.
- **Arquivos grandes com template e estilo inline.** `arquivos-versao` (559 linhas), `preco-versao` (349 ts + 455 html), `peca-do-projeto` (378 ts), `cotacao-metal` (266), `ligas-atelie` (197), `precos-projeto`, `pecas-projeto`.
- **Rotas das ferramentas.** As quatro rotas de topo repetem guards e permissões (B10).

### 3.3 Dívidas

- **Estado de ocupado das ferramentas.** `sincronizando` (string ou null) acumula três papéis (véu, trava de botão, mensagem) e é zerado à mão em 12 pontos; `trocando` e `pensando` vivem à parte. Uma fila única de operações elimina a classe inteira de corridas (A2, M1, M8, M9).
- **Tratamento de erro sem dono.** Ora o componente filtra `HttpErrorResponse` contando com o interceptor (que não cobre Blob), ora não filtra (toast duplo), ora não trata; não há `ErrorHandler` global (M34).
- **Sessão.** Tokens em localStorage (M32), sem coordenação entre abas (M31), sem renovação proativa (M4).
- **Terceiros na mesma origem.** SolidCraft (0.40.0-rc.2 em `public/solidcraft`, enquanto o script instala 0.30.1 por padrão) e CADCraft são versionados fora do git e rodam na origem do Castwork, com acesso ao localStorage dos tokens.
- **Protótipos e sobras em produção.** Etapas da versão em localStorage (B23), modo mock (B3), Documentos e Portfólio no menu e código morto (B7), READMEs desatualizados (B8).
- **Lacunas de produto registradas pelos revisores.** Cliente sem edição (B37); ateliê preserva `endereco` sem campo para editá-lo; nenhuma tela tem guarda de alterações não salvas; limites do backend ausentes no cliente (valor da hora até 10.000, cotação até 100.000); histórico de cotações com `track item.registradaEm` sem o `id` de desempate.
- **Testes (M42 a M46).** 264 testes, quase nenhum de falha; os componentes de maior risco sem spec.

### 3.4 Proposta de reorganização por domínio

1. **Ferramentas (maior retorno).** Criar `modules/ferramentas/comum/` com:
   - `ArquivoDaFerramenta`: classe ou serviço por instância de tela, com a fila de operações (abrir, trocar, salvar, restaurar, marcar final), um único estado `ocupado` com mensagem, `atualId`/`finalId` e a Promise da troca em curso;
   - interface `PonteDeEditor { carregar, temAlteracoes, salvar, descarregar }`, implementada por `PonteCadCraft` e `PonteSolidCraft`, criada por fábrica injetável (o que também torna os componentes testáveis);
   - `cw-historico-arquivo` com rótulos por input, o `.scss` da prancheta e `emBase64`;
   - `ConversaIaComponent` junto de `ferramenta-ia` (hoje em `shared/ui`), num lugar só e com o input `somenteLeitura`;
   - um pai de rota comum para as ferramentas, com guards, `canDeactivate` e validação de parâmetros.
2. **Data.** Dividir `projetos.service.ts` em `ProjetosService`, `PecasDoProjetoService` e `PrecoDaVersaoService`/`PrecosDoProjetoService`; decidir entre mover os documentos para `queries/` ou reescrever o README; `data/common` com `blobOuNulo()`, `semCorpo()` e o helper de download com erro de Blob; `dadosGql` estrito para campo raiz nulo com `errors`; tipos gerados do `schema.graphql`; remover `documentos/` e o modo mock.
3. **Shared.** Unificar `components/` e `ui/`; mover `tipo-peca.ts` para `data/pecas`; acrescentar os helpers transversais: `valorDe(resource)`, `executarComando`, `idDaRota`, formatadores `moeda`/`numero`; mover `DesenhoDaPecaComponent` para cá (ou para um domínio de peças compartilhado); `tipo-joia.ts` para o módulo do briefing.
4. **Core.** `GlobalErrorHandler` e `withNavigationErrorHandler`; política de sessão num lugar só (guard e interceptor decidindo pelo tipo do erro); `clearStore` no logout.
5. **Lint.** `no-restricted-imports` cobrindo caminhos relativos entre módulos; regra (ou item de revisão) para `resource.value()` sem guarda; knip fixado e com membros de classe e tipos.

## 4. Plano de correção

Um PR por lote, em ordem de prioridade. Cada lote traz os próprios testes; os lotes 18 e 19 cobrem o que sobrar. O lote 3 deve preceder o 17 (extração da base comum), e o 1 cria o helper que vários outros usam.

| # | Lote | Achados | Esforço |
|---|---|---|---|
| 1 | **Guarda de `resource.value()`**: helper `valorDe`, troca nos ~30 pontos, ramos de erro (conferência, briefing) e um spec de erro por tela | A1, M22 | M |
| 2 | **Correções de uma tela, baixo risco**: `sincronizando` no restaurar do 2D; `params` com `versaoId` no Duplicar e spec com roteador | A4, A5 | P |
| 3 | **Ferramentas: fila única de trocas e estado ocupado** (2D e 3D): serializar `trazerArquivo`, `trocando` como ocupado, `atualId` após sucesso, estado "aberto/falhou", `getArquivo` dentro do `try`, try/finally no `iniciar` do 2D, helper de operação | A2, M1, M8, M9, M10, B9 | M |
| 4 | **Não perder trabalho ao sair**: `canDeactivate` e `beforeunload` nas duas ferramentas, `location.replace` nos iframes, instrumentação do sintoma | A3, M2, M13 | M |
| 5 | **Sessão que renova em vez de deslogar**: interceptor decide pelo tipo do erro, guard tenta o refresh, logout idempotente e sem regravar tokens, testes de sessão | A6, M4, B28, B29, M45 | M |
| 6 | **Saída do 403 e rotas**: "Sair" na página de erro, redirect para a primeira rota permitida, `computed` no `UsuarioComponent`, `idDaRota`, recarga da ferramenta na troca de parâmetro | M5, B30, B22, B10 | M |
| 7 | **Conversa com IA resiliente**: `sincronizar(anterior)` no erro do POST, polling tolerante, compositor só limpa após sucesso, `ocupado` nas ações do chat, Desfazer só no último cartão, Apagar bloqueado durante o turno | M3, M19, M16, M21, B11 | M |
| 8 | **Erros de download e mensagens do servidor**: tratamento único de erro em Blob, `detail` fora de 400/422, estado de erro na imagem protegida e no logo, revogação tardia da object URL | M6, M33, M35, M38, B26 | M |
| 9 | **Permissões na interface**: `somenteLeitura` no chat e nas ferramentas, `podeEditar` na pasta de arquivos, botão de valor só com leitura, confirmação de peça publicada fora do modal | M12, M25, B19, M11 | M |
| 10 | **Formulários e stepper de projetos e precificação**: não repor formulário sujo, stepper com fila, reversão e flush, recargas sem piscar, commands com `error` | M23, M26, M27, M28, B17, B18, B20, B31 | M |
| 11 | **Pasta de arquivos da versão**: `rxResource` com cancelamento e estado de erro | M24 | M |
| 12 | **Dados GraphQL e SSE**: `dadosGql` estrito, nulabilidade dos DTOs, Apollo (política única, `typePolicies`, `clearStore`), cabeçalho do arquivo, parser de SSE com try/catch | M7, B4, B5, B2, B1 | M |
| 13 | **Pontes e pontas das ferramentas**: `esperandoSalvar`, flag `pendenteDeEnvio`, timeout nas chamadas ao SolidCraft, CADCraft carregado uma vez, anexos com limite e "preparando", textos por ferramenta, largura, rolagem, ids | M17, M18, M14, M20, B15, B13, B14, B16, B12 | M |
| 14 | **Componentes de shared**: PDF (cancelamento e escala), toasts, `cw-campo`, `aplicarErrosDoServidor`, painel/lightbox, estado-lista, paginação, vazamento de URL | M36, M37, M39, M40, B33, B34, B35, B36, B32 | M |
| 15 | **Briefing, visualizador de desenho, ateliê e clientes**: params estáveis do PDF, vista com falha, carga reativa do desenho, Desfazer local, subtítulo e validação do novo cliente, seletor de cliente com busca | M30, M29, B25, B27, B37, B21 | M |
| 16 | **Infraestrutura de erro e build**: `GlobalErrorHandler` e erro de chunk, budget do SCSS, bundle inicial, knip fixado, import do `heic-to` | M34, M41, B38, B39, B40 | M |
| 17 | **Extrair a base comum 2D/3D** (depois do lote 3): `ArquivoDaFerramenta`, `PonteDeEditor`, `cw-historico-arquivo` | M15 | G |
| 18 | **Testes das ferramentas e do assistente**: pontes, componentes, conversa, SSE, briefing (em parte entregue pelos lotes 3, 7, 12 e 13) | M42 | G |
| 19 | **Testes das demais áreas**: projetos, precificação/peças/ateliê, shared | M43, M44, M46 | M |
| 20 | **Limpeza e organização**: código morto e placeholders, modo mock, READMEs e regra de lint, divisão do `projetos.service`, duplicações de projetos, protótipo de etapas | B7, B3, B8, B6, B24, B23 | M |
| 21 | **Sessão, segunda etapa** (decisão de produto e backend): coordenação entre abas, refresh em cookie HttpOnly | M31, M32 | G |

Todos os 92 achados estão em algum lote. Os lotes 1 a 5 resolvem os 6 Altos e os Médios confirmados mais ligados a perda de trabalho.

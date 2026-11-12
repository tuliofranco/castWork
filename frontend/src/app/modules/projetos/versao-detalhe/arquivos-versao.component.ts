import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, input, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ArquivoDaVersaoDto, ListaDeArquivosDto, OrigemArquivo } from '@data/assistente/dto/ferramenta.dto';
import { BriefingService } from '@data/ferramentas/briefing/briefing.service';
import type { DocumentoBriefingDto } from '@data/ferramentas/briefing/dto/briefing.dto';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import { DownloadService } from '@shared/services/download.service';
import { nomeDeArquivo } from '@shared/arquivos/nome-de-arquivo';
import { PaginacaoComponent } from '@shared/ui/paginacao.component';
import { VisualizadorPdfComponent } from '@shared/ui/visualizador-pdf.component';
import { type Observable, firstValueFrom } from 'rxjs';

const POR_PAGINA = 10;

/** Nome, selo e extensão de cada ferramenta com arquivo. */
const FERRAMENTAS: Record<string, { readonly nome: string; readonly selo: string; readonly extensao: string }> = {
  'desenho-2d': { nome: 'Desenho 2D', selo: '2D', extensao: 'dxf' },
  'modelo-3d': { nome: 'Modelo 3D', selo: '3D', extensao: 'json' },
};

const ORIGENS: Record<OrigemArquivo, string> = {
  Ia: 'feito pela IA',
  Joalheiro: 'salvo por você',
  Desfazer: 'ação da IA desfeita',
  Restauracao: 'restaurado do histórico',
  Conferencia: 'conferência de materiais',
  Biblioteca: 'cópia da biblioteca',
  Montagem: 'montado a partir das peças',
  NomesDaPeca: 'marcado como final',
};

/**
 * A pasta de arquivos da versão, dentro da Composição: o que as ferramentas (2D, 3D...) gravaram. Abre um
 * modal com os arquivos, 10 por página: ver a prévia, renomear, baixar, abrir na ferramenta e arquivar.
 * Na aba Arquivados, desarquivar ou apagar de vez (com confirmação). O PDF do briefing, que é do projeto,
 * aparece fixo no topo da pasta de todas as versões.
 */
@Component({
  selector: 'cw-arquivos-versao',
  imports: [DatePipe, RouterLink, PaginacaoComponent, VisualizadorPdfComponent],
  template: `
    <button type="button" class="pasta" (click)="abrir()">
      <i class="bi bi-folder2-open" aria-hidden="true"></i>
      <span><strong>Arquivos</strong><small>Desenhos e modelos das ferramentas</small></span>
      <i class="bi bi-chevron-right seta" aria-hidden="true"></i>
    </button>

    <dialog #dialogo class="modal-arquivos" aria-labelledby="titulo-arquivos">
      <header>
        @if (vendo() || lendoBriefing()) {
          <button type="button" class="btn btn-sutil" (click)="fecharVisualizacao()"><i class="bi bi-chevron-left" aria-hidden="true"></i> Arquivos</button>
        } @else {
          <h2 id="titulo-arquivos">Arquivos da versão</h2>
        }
        <button type="button" class="btn btn-sutil btn-icone" aria-label="Fechar" (click)="dialogo.close()"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
      </header>
      <div class="corpo">
        @if (lendoBriefing(); as l) {
          <div class="visualizacao">
            <p class="titulo-visualizacao"><span class="selo">Briefing</span> {{ semExtensao(l.documento.nomeArquivo) }}</p>
            @if (l.pdf; as pdf) {
              <div class="folha-pdf"><cw-visualizador-pdf [arquivo]="pdf" [rotulo]="semExtensao(l.documento.nomeArquivo)" /></div>
            } @else {
              <p class="carregando" role="status"><span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Abrindo o PDF...</p>
            }
          </div>
        } @else if (vendo(); as v) {
          <div class="visualizacao">
            <p class="titulo-visualizacao"><span class="selo">{{ ferramenta(v.arquivo).selo }}</span> {{ nome(v.arquivo) }}</p>
            @if (v.url; as u) {
              <img [src]="u" [alt]="nome(v.arquivo)" />
            } @else if (v.url === null) {
              <p class="carregando" role="status"><span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Gerando a prévia...</p>
            } @else {
              <p class="carregando">Não foi possível gerar a prévia deste arquivo.</p>
            }
          </div>
        } @else if (pagina(); as p) {
          <div class="abas" role="tablist">
            <button type="button" role="tab" [attr.aria-selected]="!arquivados()" [class.ativa]="!arquivados()" (click)="trocarAba(false)">Arquivos</button>
            <button type="button" role="tab" [attr.aria-selected]="arquivados()" [class.ativa]="arquivados()" (click)="trocarAba(true)">Arquivados</button>
          </div>
          <ul>
            <!-- O briefing é do projeto: aparece fixo na pasta de todas as versões, sem arquivar nem renomear aqui. -->
            @if (!arquivados() && briefing(); as b) {
              <li class="fixo">
                <button type="button" class="item" (click)="lerBriefing(b)">
                  <span class="selo">Briefing</span>
                  <span class="descricao">
                    <strong>{{ semExtensao(b.nomeArquivo) }}</strong>
                    <small>Briefing do projeto · {{ b.geradoEm | date: 'dd/MM/yyyy HH:mm' }}</small>
                  </span>
                </button>
                <span class="acoes">
                  <button type="button" class="btn btn-sutil btn-icone" [attr.aria-label]="'Visualizar ' + semExtensao(b.nomeArquivo)" (click)="lerBriefing(b)">
                    <i class="bi bi-eye" aria-hidden="true"></i>
                  </button>
                  <button type="button" class="btn btn-sutil btn-icone" [attr.aria-label]="'Baixar ' + semExtensao(b.nomeArquivo)" (click)="baixarBriefing(b)">
                    <i class="bi bi-download" aria-hidden="true"></i>
                  </button>
                  <a class="btn btn-sutil btn-icone" [routerLink]="['/projetos', projetoId(), 'briefing']" aria-label="Abrir o briefing" (click)="dialogo.close()">
                    <i class="bi bi-box-arrow-up-right" aria-hidden="true"></i>
                  </a>
                </span>
              </li>
            }
            @for (arquivo of p.lista; track arquivo.id) {
              <li>
                @if (renomeando() === arquivo.id) {
                  <form class="renomear" (submit)="$event.preventDefault(); renomear(arquivo, campoNome.value)">
                    <span class="selo">{{ ferramenta(arquivo).selo }}</span>
                    <input #campoNome class="form-control" [value]="arquivo.nome ?? ''" [placeholder]="nomePadrao(arquivo)" maxlength="120" aria-label="Novo nome do arquivo" />
                    <button type="submit" class="btn btn-primary" [disabled]="ocupado()">Salvar</button>
                    <button type="button" class="btn btn-sutil" (click)="renomeando.set(null)">Cancelar</button>
                  </form>
                } @else if (apagando() === arquivo.id) {
                  <span class="confirmar">
                    <span>Apagar <strong>{{ nome(arquivo) }}</strong> de vez? Não dá para desfazer.</span>
                    <button type="button" class="btn btn-sutil" (click)="apagando.set(null)">Cancelar</button>
                    <button type="button" class="btn btn-perigo-sutil" [disabled]="ocupado()" (click)="apagar(arquivo)">Apagar</button>
                  </span>
                } @else {
                <button type="button" class="item" (click)="visualizar(arquivo)">
                <span class="selo">{{ ferramenta(arquivo).selo }}</span>
                <span class="descricao">
                  <strong>{{ nome(arquivo) }}</strong>
                  <small>{{ nomeDoArquivo(arquivo) }} · {{ origem(arquivo) }} · {{ arquivo.criadoEm | date: 'dd/MM/yyyy HH:mm' }}</small>
                </span>
                </button>
                <span class="acoes">
                  <button type="button" class="btn btn-sutil btn-icone" [attr.aria-label]="'Visualizar ' + nome(arquivo)" (click)="visualizar(arquivo)">
                    <i class="bi bi-eye" aria-hidden="true"></i>
                  </button>
                  <button type="button" class="btn btn-sutil btn-icone" [attr.aria-label]="'Baixar ' + nome(arquivo)" (click)="baixar(arquivo)">
                    <i class="bi bi-download" aria-hidden="true"></i>
                  </button>
                  @if (arquivados()) {
                    @if (podeEditar()) {
                    <button type="button" class="btn btn-sutil btn-icone" [attr.aria-label]="'Desarquivar ' + nome(arquivo)" [disabled]="ocupado()" (click)="desarquivar(arquivo)">
                      <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>
                    </button>
                    }
                    @if (podeEditar()) {
                    <button type="button" class="btn btn-perigo-sutil btn-icone" [attr.aria-label]="'Apagar ' + nome(arquivo)" (click)="apagando.set(arquivo.id)">
                      <i class="bi bi-trash3" aria-hidden="true"></i>
                    </button>
                    }
                  } @else {
                    @if (podeEditar()) {
                    <button type="button" class="btn btn-sutil btn-icone" [attr.aria-label]="'Renomear ' + nome(arquivo)" (click)="renomeando.set(arquivo.id)">
                      <i class="bi bi-pencil" aria-hidden="true"></i>
                    </button>
                    }
                    <a class="btn btn-sutil btn-icone" [routerLink]="arquivo.ferramenta" [attr.aria-label]="'Abrir no ' + ferramenta(arquivo).nome" (click)="dialogo.close()">
                      <i class="bi bi-box-arrow-up-right" aria-hidden="true"></i>
                    </a>
                    @if (podeEditar()) {
                    <button type="button" class="btn btn-sutil btn-icone" [attr.aria-label]="'Arquivar ' + nome(arquivo)" [disabled]="ocupado()" (click)="arquivar(arquivo)">
                      <i class="bi bi-archive" aria-hidden="true"></i>
                    </button>
                    }
                  }
                </span>
                }
              </li>
            } @empty {
              <li class="vazio">
                {{ arquivados() ? 'Nenhum arquivo arquivado.' : 'Nenhum arquivo ainda. O que for desenhado nas ferramentas aparece aqui.' }}
              </li>
            }
          </ul>
          @if (p.totalRegistros > 0) {
            <cw-paginacao
              [total]="p.totalRegistros"
              [pagina]="p.pagina"
              [singular]="arquivados() ? 'arquivado' : 'arquivo'"
              [plural]="arquivados() ? 'arquivados' : 'arquivos'"
              [temAnterior]="p.pagina > 1"
              [temProxima]="p.pagina < p.totalPaginas"
              (anterior)="carregar(p.pagina - 1)"
              (proxima)="carregar(p.pagina + 1)"
            />
          }
        } @else {
          <p class="carregando" role="status"><span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Carregando...</p>
        }
      </div>
    </dialog>
  `,
  styles: `
    .pasta {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      width: 100%;
      min-height: 3.5rem;
      margin-bottom: 0.75rem;
      padding: 0.625rem 1rem 0.625rem 0.75rem;
      border: 1px solid var(--cw-border);
      border-radius: var(--cw-radius-lg);
      background: transparent;
      color: var(--cw-ink);
      text-align: left;

      > .bi:first-child {
        display: grid;
        place-items: center;
        width: 2.5rem;
        height: 2.5rem;
        border-radius: var(--cw-radius);
        background: var(--cw-surface-2);
        font-size: 1.25rem;
      }

      span {
        display: flex;
        flex: 1;
        flex-direction: column;
      }

      small {
        color: var(--cw-ink-muted);
      }

      .seta {
        color: var(--cw-ink-muted);
      }
    }

    .modal-arquivos {
      width: min(48rem, 100vw - 2rem);
      max-height: min(44rem, 100dvh - 2rem);
      padding: 0;
      border: 1px solid var(--cw-border);
      border-radius: 0.75rem;
      background: var(--cw-surface);
      color: var(--cw-ink);

      &::backdrop {
        background: oklch(0.15 0.01 60 / 0.6);
      }

      header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 0.75rem 1rem;
        border-bottom: 1px solid var(--cw-border);

        h2 {
          margin: 0;
          font-size: 1.0625rem;
          font-weight: 600;
        }
      }
    }

    .corpo {
      padding: 0.5rem 1rem 1rem;
    }

    ul {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    li {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.625rem 0;
      border-bottom: 1px solid var(--cw-border);
    }

    .selo {
      flex: none;
      min-width: 2.5rem;
      padding: 0.125rem 0.5rem;
      border-radius: 999px;
      background: var(--cw-surface-2);
      font-size: 0.75rem;
      font-weight: 600;
      text-align: center;
    }

    .descricao {
      display: flex;
      flex: 1;
      flex-direction: column;
      min-width: 0;

      small {
        color: var(--cw-ink-muted);
      }
    }

    .acoes {
      display: flex;
      gap: 0.25rem;

      .btn {
        min-height: var(--cw-alvo);
      }
    }

    .vazio {
      justify-content: center;
      color: var(--cw-ink-muted);
      border-bottom: 0;
    }

    .abas {
      display: flex;
      gap: 0.25rem;
      margin-bottom: 0.25rem;
      border-bottom: 1px solid var(--cw-border);

      button {
        min-height: var(--cw-alvo);
        padding: 0 0.75rem;
        border: 0;
        border-bottom: 2px solid transparent;
        background: transparent;
        color: var(--cw-ink-muted);
      }

      .ativa {
        border-bottom-color: currentcolor;
        color: var(--cw-ink);
        font-weight: 600;
      }
    }

    .renomear,
    .confirmar {
      display: flex;
      flex: 1;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.5rem;

      input {
        flex: 1;
        min-width: 10rem;
      }

      > span:first-child:not(.selo) {
        flex: 1;
        min-width: 12rem;
      }
    }

    .item {
      display: flex;
      flex: 1;
      align-items: center;
      gap: 0.75rem;
      min-width: 0;
      padding: 0;
      border: 0;
      background: transparent;
      color: inherit;
      text-align: left;
    }

    .visualizacao {
      padding-top: 0.5rem;

      /* O SVG do CADCraft tem linhas escuras em fundo branco: a folha é branca nos dois temas. */
      img {
        display: block;
        width: 100%;
        height: min(30rem, 60dvh);
        padding: 0.75rem;
        border: 1px solid var(--cw-border);
        border-radius: var(--cw-radius-lg);
        background: #fff;
        object-fit: contain;
      }
    }

    .folha-pdf {
      max-height: min(36rem, 70dvh);
      overflow: auto;
    }

    .fixo .selo {
      background: var(--cw-accent-soft, var(--cw-surface-2));
    }

    .titulo-visualizacao {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin: 0 0 0.75rem;
      font-weight: 600;
    }

    .carregando {
      margin: 1rem 0;
      color: var(--cw-ink-muted);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ArquivosVersaoComponent {
  private readonly ferramentas = inject(FerramentasService);
  private readonly download = inject(DownloadService);
  private readonly briefings = inject(BriefingService);

  readonly projetoId = input.required<number>();
  readonly versaoId = input.required<number>();
  /** `projetos.editar`: sem a regra, a pasta é só para ver e baixar (sem renomear, arquivar nem apagar). */
  readonly podeEditar = input(false);

  protected readonly pagina = signal<ListaDeArquivosDto | null>(null);
  protected readonly arquivados = signal(false);
  /** Arquivo com o campo de nome aberto. */
  protected readonly renomeando = signal<number | null>(null);
  /** Arquivo pedindo a confirmação de apagar. */
  protected readonly apagando = signal<number | null>(null);
  protected readonly ocupado = signal(false);
  /** O PDF mais recente do briefing do projeto; nulo se ainda não foi gerado. */
  protected readonly briefing = signal<DocumentoBriefingDto | null>(null);
  /** PDF do briefing aberto no modal; `pdf` nulo enquanto baixa. */
  protected readonly lendoBriefing = signal<{ readonly documento: DocumentoBriefingDto; readonly pdf: Blob | null } | null>(null);
  /** Arquivo aberto no modal; `url` nula enquanto a prévia é gerada e falsa se ela não veio. */
  protected readonly vendo = signal<{ readonly arquivo: ArquivoDaVersaoDto; readonly url: string | null | false } | null>(null);
  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  constructor() {
    inject(DestroyRef).onDestroy(() => this.fecharVisualizacao());
  }

  protected abrir(): void {
    this.fecharVisualizacao();
    this.arquivados.set(false);
    this.pagina.set(null);
    this.dialogo().nativeElement.showModal();
    void this.carregar(1);
    void this.carregarBriefing();
  }

  private async carregarBriefing(): Promise<void> {
    try {
      const briefing = await firstValueFrom(this.briefings.getBriefing(this.projetoId()));
      this.briefing.set(briefing?.documentos[0] ?? null);
    } catch {
      // Sem briefing a pasta mostra só os arquivos da versão.
      this.briefing.set(null);
    }
  }

  protected async lerBriefing(documento: DocumentoBriefingDto): Promise<void> {
    this.fecharVisualizacao();
    this.lendoBriefing.set({ documento, pdf: null });
    try {
      const pdf = await firstValueFrom(this.briefings.getDocumento(this.projetoId(), documento.id));
      if (this.lendoBriefing()?.documento === documento) this.lendoBriefing.set({ documento, pdf });
    } catch {
      this.lendoBriefing.set(null);
    }
  }

  protected async baixarBriefing(documento: DocumentoBriefingDto): Promise<void> {
    this.download.salvar(await firstValueFrom(this.briefings.getDocumento(this.projetoId(), documento.id)), documento.nomeArquivo);
  }

  protected semExtensao(nomeArquivo: string): string {
    return nomeArquivo.replace(/\.[^.]+$/, '');
  }

  protected async carregar(pagina: number): Promise<void> {
    this.renomeando.set(null);
    this.apagando.set(null);
    const lista = await firstValueFrom(this.ferramentas.getArquivosDaVersao(this.projetoId(), this.versaoId(), this.arquivados(), pagina, POR_PAGINA));
    // A última página pode ficar vazia depois de arquivar ou apagar: volta uma.
    if (lista.lista.length === 0 && pagina > 1) return this.carregar(pagina - 1);
    this.pagina.set(lista);
  }

  protected trocarAba(arquivados: boolean): void {
    if (this.arquivados() === arquivados) return;
    this.arquivados.set(arquivados);
    this.pagina.set(null);
    void this.carregar(1);
  }

  protected renomear(arquivo: ArquivoDaVersaoDto, nome: string): Promise<void> {
    return this.executar(this.ferramentas.putNomeDoArquivo(this.projetoId(), this.versaoId(), arquivo.ferramenta, arquivo.id, nome.trim() || null));
  }

  protected arquivar(arquivo: ArquivoDaVersaoDto): Promise<void> {
    return this.executar(this.ferramentas.postArquivar(this.projetoId(), this.versaoId(), arquivo.ferramenta, arquivo.id));
  }

  protected desarquivar(arquivo: ArquivoDaVersaoDto): Promise<void> {
    return this.executar(this.ferramentas.postDesarquivar(this.projetoId(), this.versaoId(), arquivo.ferramenta, arquivo.id));
  }

  protected apagar(arquivo: ArquivoDaVersaoDto): Promise<void> {
    return this.executar(this.ferramentas.deleteArquivo(this.projetoId(), this.versaoId(), arquivo.ferramenta, arquivo.id));
  }

  /** Erros da API já viram toast no interceptor; a lista recarrega na mesma página. */
  private async executar(acao: Observable<void>): Promise<void> {
    this.ocupado.set(true);
    try {
      await firstValueFrom(acao);
      await this.carregar(this.pagina()?.pagina ?? 1);
    } catch {
      // O toast já avisou.
    } finally {
      this.ocupado.set(false);
    }
  }

  protected ferramenta(arquivo: ArquivoDaVersaoDto): { readonly nome: string; readonly selo: string; readonly extensao: string } {
    return FERRAMENTAS[arquivo.ferramenta] ?? { nome: arquivo.ferramenta, selo: '?', extensao: 'bin' };
  }

  /** Sem extensão na tela: a extensão só entra no nome do arquivo baixado. */
  protected nome(arquivo: ArquivoDaVersaoDto): string {
    return arquivo.nome ?? this.nomePadrao(arquivo);
  }

  /** O nome de verdade do arquivo, com extensão: o mesmo que o editor usa ("Visão 2d" → "visao-2d.dxf"). */
  protected nomeDoArquivo(arquivo: ArquivoDaVersaoDto): string {
    return nomeDeArquivo(this.nome(arquivo), this.ferramenta(arquivo).extensao);
  }

  protected nomePadrao(arquivo: ArquivoDaVersaoDto): string {
    return `${this.ferramenta(arquivo).nome} #${arquivo.id}`;
  }

  protected origem(arquivo: ArquivoDaVersaoDto): string {
    const texto = ORIGENS[arquivo.origem];
    return texto.charAt(0).toUpperCase() + texto.slice(1);
  }

  protected async visualizar(arquivo: ArquivoDaVersaoDto): Promise<void> {
    this.fecharVisualizacao();
    this.vendo.set({ arquivo, url: null });
    let url: string | false = false;
    try {
      const svg = await firstValueFrom(this.ferramentas.getPrevia(this.projetoId(), this.versaoId(), arquivo.ferramenta, arquivo.id));
      if (svg) url = URL.createObjectURL(svg);
    } catch {
      // Erro da API já vira toast no interceptor; aqui só sai o "carregando".
    }

    if (this.vendo()?.arquivo === arquivo) this.vendo.set({ arquivo, url });
    else if (url) URL.revokeObjectURL(url);
  }

  protected fecharVisualizacao(): void {
    this.lendoBriefing.set(null);
    const url = this.vendo()?.url;
    if (url) URL.revokeObjectURL(url);
    this.vendo.set(null);
  }

  protected async baixar(arquivo: ArquivoDaVersaoDto): Promise<void> {
    const conteudo = await firstValueFrom(this.ferramentas.getVersaoDoArquivo(this.projetoId(), this.versaoId(), arquivo.ferramenta, arquivo.id));
    if (conteudo) this.download.salvar(conteudo, this.nomeDoArquivo(arquivo));
  }
}

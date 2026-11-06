import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { BriefingService } from '@data/projetos/briefing/briefing.service';
import type { VersaoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { type Observable, finalize } from 'rxjs';

import { STATUS_VERSAO } from '../status-versao';

/**
 * Página do projeto: as versões (criar, publicar, arquivar). Cliente e briefing ficam nas configurações;
 * aqui só a etiqueta do cliente e um lembrete enquanto o briefing não existe.
 */
@Component({
  selector: 'cw-projeto-detalhe',
  imports: [DatePipe, FormsModule, RouterLink, CabecalhoPaginaComponent, EstadoListaComponent, SeloComponent],
  templateUrl: './projeto-detalhe.component.html',
  styleUrl: './projeto-detalhe.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjetoDetalheComponent {
  private readonly projetosService = inject(ProjetosService);
  private readonly briefingService = inject(BriefingService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  /** `:id` da rota (withComponentInputBinding). */
  readonly id = input.required<string>();

  protected readonly podeEditar = inject(PermissaoService).temAlguma(['projetos.editar']);
  protected readonly status = STATUS_VERSAO;
  protected readonly salvando = signal(false);
  protected readonly mostrarArquivadas = signal(false);

  protected readonly projeto = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.projetosService.getProjeto(params),
  });

  protected readonly briefing = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.briefingService.getBriefing(params),
  });

  protected readonly arquivadas = computed(() => (this.projeto.value()?.versoes ?? []).filter((v) => v.status === 'ARQUIVADA'));
  /** Lista ordenada pelo número da versão, da primeira para a mais recente. */
  protected readonly versoesVisiveis = computed(() =>
    (this.projeto.value()?.versoes ?? [])
      .filter((v) => this.mostrarArquivadas() || v.status !== 'ARQUIVADA')
      .sort((a, b) => a.numero - b.numero),
  );

  /** Formulário inline de nova versão: o nome é livre e obrigatório; o número vem do servidor. */
  protected readonly criandoVersao = signal(false);
  protected readonly nomeNovaVersao = signal('');

  protected abrirNovaVersao(): void {
    this.nomeNovaVersao.set('');
    this.criandoVersao.set(true);
  }

  protected criarVersao(): void {
    const nome = this.nomeNovaVersao().trim();
    if (!nome) return;
    this.salvando.set(true);
    this.projetosService
      .postVersao(this.projetoId(), { nome })
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe(({ id }) => {
        this.toast.sucesso(`Versão "${nome}" criada.`);
        void this.router.navigate(['/projetos', this.projetoId(), 'versoes', id]);
      });
  }

  protected publicar(versao: VersaoProjetoDto): void {
    this.executar(this.projetosService.postPublicarVersao(this.projetoId(), versao.id), `Versão ${versao.numero} publicada.`);
  }

  protected arquivar(versao: VersaoProjetoDto): void {
    this.executar(this.projetosService.postArquivarVersao(this.projetoId(), versao.id), `Versão ${versao.numero} arquivada.`);
  }

  protected desarquivar(versao: VersaoProjetoDto): void {
    this.executar(this.projetosService.postDesarquivarVersao(this.projetoId(), versao.id), `Versão ${versao.numero} de volta ao desenvolvimento.`);
  }

  private projetoId(): number {
    return Number(this.id());
  }

  /** Executa o command e relê o projeto (erros 400/422 já viram toast no interceptor). */
  private executar(comando: Observable<void>, sucesso: string): void {
    this.salvando.set(true);
    comando.pipe(finalize(() => this.salvando.set(false))).subscribe({
      next: () => {
        this.toast.sucesso(sucesso);
        this.projeto.reload();
      },
      error: () => this.projeto.reload(),
    });
  }
}

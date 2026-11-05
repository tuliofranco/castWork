import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import type { VersaoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { type Observable, finalize, of } from 'rxjs';

import { STATUS_VERSAO } from '../status-versao';

/**
 * Página do projeto: o cliente vinculado e a lista de versões (criar, publicar, arquivar).
 * A composição de cada versão fica na página da versão.
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
  private readonly clientesService = inject(ClientesService);
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

  protected readonly clientes = rxResource({
    params: () => this.podeEditar,
    stream: ({ params }) => (params ? this.clientesService.getClientesOpcoes() : of([])),
  });

  /** Cliente escolhido no seletor; volta ao do projeto sempre que o projeto é relido. */
  protected readonly clienteEscolhido = linkedSignal<number | null>(() => this.projeto.value()?.cliente?.id ?? null);
  protected readonly clienteMudou = computed(() => this.clienteEscolhido() !== (this.projeto.value()?.cliente?.id ?? null));

  protected readonly arquivadas = computed(() => (this.projeto.value()?.versoes ?? []).filter((v) => v.status === 'ARQUIVADA'));
  protected readonly versoesVisiveis = computed(() =>
    (this.projeto.value()?.versoes ?? []).filter((v) => this.mostrarArquivadas() || v.status !== 'ARQUIVADA'),
  );

  protected salvarCliente(): void {
    const clienteId = this.clienteEscolhido();
    this.executar(this.projetosService.putCliente(this.projetoId(), clienteId), clienteId === null ? 'Projeto desvinculado do cliente.' : 'Cliente vinculado.');
  }

  protected novaVersao(): void {
    const proximo = Math.max(0, ...(this.projeto.value()?.versoes ?? []).map((v) => v.numero)) + 1;
    this.salvando.set(true);
    this.projetosService
      .postVersao(this.projetoId(), { nome: `Versão ${proximo}` })
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe(({ id }) => {
        this.toast.sucesso(`Versão ${proximo} criada.`);
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

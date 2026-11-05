import { DecimalPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import type { PecaDto } from '@data/pecas/pecas/dto/peca.dto';
import { PecasService } from '@data/pecas/pecas/pecas.service';
import type { ItemComposicaoDto, VersaoProjetoDto } from '@data/projetos/projetos/dto/projeto.dto';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { formatarTeor, rotuloTipoPeca } from '@shared/pecas/tipo-peca';
import { ToastService } from '@shared/services/toast.service';
import { type Observable, debounceTime, distinctUntilChanged, finalize, of } from 'rxjs';

import { STATUS_VERSAO } from '../status-versao';

/**
 * Detalhe do projeto: versões em abas, composição da versão escolhida e, enquanto ela estiver em desenvolvimento,
 * a busca na biblioteca para adicionar peças. Cada ação chama o REST e relê o projeto pelo GraphQL.
 */
@Component({
  selector: 'cw-projeto-detalhe',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, DatePipe],
  templateUrl: './projeto-detalhe.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjetoDetalheComponent {
  private readonly projetosService = inject(ProjetosService);
  private readonly pecasService = inject(PecasService);
  private readonly toast = inject(ToastService);

  /** `:id` da rota (withComponentInputBinding). */
  readonly id = input.required<string>();

  protected readonly podeEditar = inject(PermissaoService).temAlguma(['projetos.editar']);
  protected readonly status = STATUS_VERSAO;
  protected readonly rotuloTipo = rotuloTipoPeca;
  protected readonly formatarTeor = formatarTeor;
  protected readonly salvando = signal(false);

  protected readonly projeto = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.projetosService.getProjeto(params),
  });

  /** Versão escolhida nas abas; sem escolha, a mais recente. */
  private readonly versaoEscolhida = signal<number | null>(null);
  protected readonly versao = computed<VersaoProjetoDto | undefined>(() => {
    const versoes = this.projeto.value()?.versoes ?? [];
    return versoes.find((v) => v.id === this.versaoEscolhida()) ?? versoes[0];
  });
  protected readonly editavel = computed(() => this.podeEditar && this.versao()?.status === 'EM_DESENVOLVIMENTO');

  /** Soma do peso das peças que têm peso informado (quantidade × peso). */
  protected readonly pesoTotal = computed(() =>
    (this.versao()?.itens ?? []).reduce((total, item) => total + item.quantidade * (item.peca.pesoGramas ?? 0), 0),
  );

  protected readonly buscaPeca = new FormControl('', { nonNullable: true });
  private readonly termoPeca = toSignal(this.buscaPeca.valueChanges.pipe(debounceTime(300), distinctUntilChanged()), { initialValue: '' });
  protected readonly resultadosPeca = rxResource({
    params: () => ({ termo: this.termoPeca(), editavel: this.editavel() }),
    stream: ({ params }) =>
      params.editavel ? this.pecasService.getPecas({ termo: params.termo || null, primeiros: 10, somenteAtivas: true }) : of(null),
  });

  protected escolherVersao(versao: VersaoProjetoDto): void {
    this.versaoEscolhida.set(versao.id);
  }

  protected adicionar(peca: PecaDto, quantidade: string): void {
    const versao = this.versao();
    const qtd = Number(quantidade);
    if (!versao || !Number.isInteger(qtd) || qtd < 1) {
      this.toast.aviso('Informe uma quantidade inteira a partir de 1.');
      return;
    }
    this.executar(this.projetosService.postItem(this.projetoId(), versao.id, { pecaId: peca.id, quantidade: qtd }), `${peca.nome} adicionada.`);
  }

  protected alterarQuantidade(item: ItemComposicaoDto, valor: string): void {
    const versao = this.versao();
    const qtd = Number(valor);
    if (!versao || qtd === item.quantidade) return;
    if (!Number.isInteger(qtd) || qtd < 1) {
      this.toast.aviso('A quantidade deve ser inteira, a partir de 1.');
      this.projeto.reload();
      return;
    }
    this.executar(
      this.projetosService.putItem(this.projetoId(), versao.id, item.id, { quantidade: qtd, observacao: item.observacao }),
      null,
    );
  }

  protected remover(item: ItemComposicaoDto): void {
    const versao = this.versao();
    if (versao) this.executar(this.projetosService.deleteItem(this.projetoId(), versao.id, item.id), `${item.peca.nome} removida.`);
  }

  protected publicar(): void {
    const versao = this.versao();
    if (versao) this.executar(this.projetosService.postPublicarVersao(this.projetoId(), versao.id), `Versão ${versao.numero} publicada.`);
  }

  protected duplicar(): void {
    const versao = this.versao();
    if (versao) {
      this.executar(this.projetosService.postDuplicarVersao(this.projetoId(), versao.id), 'Nova versão criada a partir desta.', (id) =>
        this.versaoEscolhida.set(id),
      );
    }
  }

  protected novaVersao(): void {
    const proximo = Math.max(0, ...(this.projeto.value()?.versoes ?? []).map((v) => v.numero)) + 1;
    this.executar(this.projetosService.postVersao(this.projetoId(), { nome: `Versão ${proximo}` }), `Versão ${proximo} criada.`, (id) =>
      this.versaoEscolhida.set(id),
    );
  }

  private projetoId(): number {
    return Number(this.id());
  }

  /**
   * Executa o command e relê o projeto. Erros (422 de versão publicada, 400 de validação) já viram toast
   * no erroGlobalInterceptor; aqui só se relê para a tela voltar ao estado real.
   */
  private executar<T>(comando: Observable<T>, sucesso: string | null, depois?: (id: number) => void): void {
    this.salvando.set(true);
    comando.pipe(finalize(() => this.salvando.set(false))).subscribe({
      next: (resposta) => {
        if (sucesso) this.toast.sucesso(sucesso);
        const id = (resposta as { id?: number } | undefined)?.id;
        if (depois && id !== undefined) depois(id);
        this.projeto.reload();
      },
      error: () => this.projeto.reload(),
    });
  }
}

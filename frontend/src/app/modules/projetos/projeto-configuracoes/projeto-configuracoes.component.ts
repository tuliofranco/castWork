import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { BriefingService } from '@data/ferramentas/briefing/briefing.service';
import { ProjetosService } from '@data/projetos/projetos/projetos.service';
import { ToastService } from '@shared/services/toast.service';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { PainelPdfComponent } from '@shared/ui/painel-pdf.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { finalize, of } from 'rxjs';

/**
 * Configurações do projeto: o que se define uma vez e raramente muda (nome, cliente e briefing).
 * Definidos, aparecem como uma linha discreta; o seletor e as ações só abrem quando pedidos.
 */
@Component({
  selector: 'cw-projeto-configuracoes',
  imports: [DatePipe, FormsModule, RouterLink, CabecalhoPaginaComponent, EstadoListaComponent, PainelPdfComponent, SeloComponent],
  templateUrl: './projeto-configuracoes.component.html',
  styleUrl: './projeto-configuracoes.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjetoConfiguracoesComponent {
  private readonly projetosService = inject(ProjetosService);
  private readonly clientesService = inject(ClientesService);
  private readonly briefingService = inject(BriefingService);
  private readonly toast = inject(ToastService);

  /** `:id` da rota (withComponentInputBinding). */
  readonly id = input.required<string>();

  protected readonly podeEditar = inject(PermissaoService).temAlguma(['projetos.editar']);
  protected readonly salvando = signal(false);

  protected readonly projeto = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.projetosService.getProjeto(params),
  });

  protected readonly briefing = rxResource({
    params: () => Number(this.id()),
    stream: ({ params }) => this.briefingService.getBriefing(params),
  });

  protected readonly clientes = rxResource({
    params: () => this.podeEditar,
    stream: ({ params }) => (params ? this.clientesService.getClientesOpcoes() : of([])),
  });

  /** Mesmo limite do backend (`Projeto.NomeMaxLength`). */
  protected readonly nomeMaximo = 150;
  /** Nome digitado; volta ao do projeto sempre que o projeto é relido. */
  protected readonly nome = linkedSignal(() => this.projeto.value()?.nome ?? '');
  protected readonly editandoNome = signal(false);
  protected readonly nomeValido = computed(() => {
    const nome = this.nome().trim();
    return nome.length > 0 && nome !== this.projeto.value()?.nome;
  });

  /** Cliente escolhido no seletor; volta ao do projeto sempre que o projeto é relido. */
  protected readonly clienteEscolhido = linkedSignal<number | null>(() => this.projeto.value()?.cliente?.id ?? null);
  /** Com cliente vinculado, o seletor só aparece ao pedir para trocar. */
  protected readonly trocandoCliente = signal(false);
  protected readonly clienteMudou = computed(() => this.clienteEscolhido() !== (this.projeto.value()?.cliente?.id ?? null));

  /** PDF do briefing aberto no painel de tela cheia. */
  protected readonly documentoAberto = signal<{ readonly projetoId: number; readonly id: number; readonly nome: string; readonly titulo: string; readonly arquivo: Blob } | null>(
    null,
  );

  protected cancelarNome(): void {
    this.nome.set(this.projeto.value()?.nome ?? '');
    this.editandoNome.set(false);
  }

  protected salvarNome(): void {
    if (!this.nomeValido()) {
      return;
    }
    this.salvando.set(true);
    this.projetosService
      .putNome(Number(this.id()), this.nome().trim())
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe(() => {
        this.toast.sucesso('Nome do projeto alterado.');
        this.editandoNome.set(false);
        this.projeto.reload();
      });
  }

  protected cancelarCliente(): void {
    this.clienteEscolhido.set(this.projeto.value()?.cliente?.id ?? null);
    this.trocandoCliente.set(false);
  }

  protected salvarCliente(): void {
    const clienteId = this.clienteEscolhido();
    this.salvando.set(true);
    this.projetosService
      .putCliente(Number(this.id()), clienteId)
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe(() => {
        this.toast.sucesso(clienteId === null ? 'Projeto desvinculado do cliente.' : 'Cliente vinculado.');
        this.trocandoCliente.set(false);
        this.projeto.reload();
      });
  }

  protected verDocumento(projetoId: number, id: number, nome: string, geradoEm: string): void {
    this.briefingService.getDocumento(projetoId, id).subscribe((arquivo) =>
      this.documentoAberto.set({
        projetoId,
        id,
        nome,
        titulo: `Briefing de ${new Date(geradoEm).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}`,
        arquivo,
      }),
    );
  }

  protected baixarDocumento(projetoId: number, id: number, nome: string): void {
    this.briefingService.getDocumento(projetoId, id).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = nome;
      link.click();
      URL.revokeObjectURL(url);
    });
  }

  /** Orçamento numérico vira moeda (4000 → R$ 4.000,00); texto livre ("até 3 mil") fica como está. */
  protected valor(orcamento: string): string {
    const numero = Number(orcamento.replace(/\./g, '').replace(',', '.'));
    return Number.isFinite(numero) && /^[\d.,\s]+$/.test(orcamento)
      ? numero.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
      : orcamento;
  }
}

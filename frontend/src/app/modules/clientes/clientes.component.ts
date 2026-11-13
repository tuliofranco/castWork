import { valorDe } from '@shared/recursos/valor-de';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PermissaoService } from '@core/services/permissao.service';
import type { ClienteResumoDto } from '@data/clientes/clientes/dto/cliente-resumo.dto';
import { ClientesService } from '@data/clientes/clientes/clientes.service';
import { PaginacaoCursor } from '@shared/components/paginacao/paginacao-cursor';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoBuscaComponent } from '@shared/ui/campo-busca.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { PaginacaoComponent } from '@shared/ui/paginacao.component';
import { IniciaisComponent } from '@shared/ui/iniciais.component';
import { SeloComponent } from '@shared/ui/selo.component';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs';

const POR_PAGINA = 20;

@Component({
  selector: 'cw-clientes',
  imports: [RouterLink, CabecalhoPaginaComponent, CampoBuscaComponent, EstadoListaComponent, PaginacaoComponent, IniciaisComponent, SeloComponent],
  templateUrl: './clientes.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientesComponent {
  protected readonly valorDe = valorDe;
  private readonly clientesService = inject(ClientesService);
  protected readonly podeCriar = inject(PermissaoService).temAlguma(['clientes.criar']);
  protected readonly paginacao = new PaginacaoCursor();

  protected readonly busca = new FormControl('', { nonNullable: true });

  private readonly termo = toSignal(
    this.busca.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      tap(() => this.paginacao.reiniciar()),
    ),
    { initialValue: '' },
  );

  protected readonly clientes = rxResource({
    params: () => ({ termo: this.termo() || null, apos: this.paginacao.apos(), primeiros: POR_PAGINA }),
    stream: ({ params }) => this.clientesService.getClientes(params),
  });

  /** Documento, telefone e e-mail que existirem, separados por ponto. */
  protected contato(cliente: ClienteResumoDto): string {
    return [cliente.documento, cliente.telefone, cliente.email].filter(Boolean).join(' · ') || 'Sem contato cadastrado';
  }
}

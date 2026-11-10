import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FerramentasService } from '@data/assistente/ferramentas.service';
import type {
  ConferenciaDoModeloDto,
  CorpoConferidoDto,
  ModeloAlteradoDto,
  SobreposicaoConferidaDto,
} from '@data/ferramentas/modelo-3d/dto/conferencia-do-modelo.dto';
import { Modelo3dService } from '@data/ferramentas/modelo-3d/modelo-3d.service';
import { ToastService } from '@shared/services/toast.service';
import { type Observable, firstValueFrom } from 'rxjs';

const FERRAMENTA = 'modelo-3d';

/** 5 → "5,00 g"; 0.053 → "0,053 g". */
export function gramas(valor: number): string {
  return `${valor.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 3 })} g`;
}

/** 322.581 → "322,6 mm³". */
export function milimetrosCubicos(valor: number): string {
  return `${valor.toLocaleString('pt-BR', { maximumFractionDigits: 1 })} mm³`;
}

/** Uma sobreposição como aparece na tela: o que dá para fazer com ela. */
interface SobreposicaoNaTela {
  readonly sobreposicao: SobreposicaoConferidaDto;
  /** O maior primeiro: ele dá o nome ao corpo unido. */
  readonly corpos: readonly [string, string];
  /** Por que não dá para unir aqui (nulo quando dá). */
  readonly impedimento: string | null;
}

/**
 * Conferência de peso e materiais do modelo 3D da versão: os corpos com material, volume e peso, os sem material em
 * destaque para escolher ali mesmo (um ou vários, e Aplicar), e as sobreposições de metal com "Unir corpos" (pede
 * confirmação). Cada alteração vira uma nova versão do arquivo, com Desfazer (restaura a de antes). A tela não cria nem
 * apaga corpos e não mexe na forma (isso é no Modelo 3D). Abre quando `aberta`; avisa o Modelo 3D (`alterado`) com o
 * arquivo novo, para ele recarregar.
 */
@Component({
  selector: 'cw-conferencia-modelo',
  templateUrl: './conferencia-modelo.component.html',
  styleUrl: './conferencia-modelo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConferenciaModeloComponent {
  private readonly modelo3d = inject(Modelo3dService);
  private readonly ferramentas = inject(FerramentasService);
  private readonly toast = inject(ToastService);

  readonly projetoId = input.required<number>();
  readonly versaoId = input.required<number>();
  readonly aberta = input(false);
  /** `projetos.editar`: sem a regra, só leitura. */
  readonly podeEditar = input(false);
  /** A IA está trabalhando no modelo (ou ele está sendo trocado): espera para alterar. */
  readonly ocupado = input(false);
  /** O arquivo 3D atual mudou aqui (aplicar, unir ou desfazer): o Id do novo atual. */
  readonly alterado = output<number>();
  readonly fechado = output();

  protected readonly gramas = gramas;
  protected readonly milimetrosCubicos = milimetrosCubicos;
  protected readonly salvando = signal(false);
  /** Material escolhido e ainda não aplicado, por corpo. */
  protected readonly escolhas = signal<Readonly<Record<string, string>>>({});
  /** A sobreposição cujo "Unir corpos" espera confirmação. */
  protected readonly confirmandoUnir = signal<SobreposicaoNaTela | null>(null);
  /** A última alteração feita aqui, para o Desfazer. */
  protected readonly ultima = signal<{ readonly descricao: string; readonly anteriorId: number } | null>(null);

  private readonly recarga = signal(0);
  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  protected readonly conferencia = rxResource({
    params: () => (this.aberta() ? { projetoId: this.projetoId(), versaoId: this.versaoId(), recarga: this.recarga() } : undefined),
    stream: ({ params }) => this.modelo3d.getConferencia(params.projetoId, params.versaoId),
  });

  protected readonly editavel = computed(() => this.podeEditar() && !!this.conferencia.value()?.alteravel);

  protected readonly semMaterial = computed(() => (this.conferencia.value()?.corpos ?? []).filter((c) => c.uso === 'SEM_MATERIAL').length);

  protected readonly pendentes = computed(() => Object.keys(this.escolhas()).length);

  /** As sobreposições que importam para o peso: entre corpos de metal ou sem material (pedra e apoio ficam fora). */
  protected readonly sobreposicoes = computed<readonly SobreposicaoNaTela[]>(() => {
    const conferencia = this.conferencia.value();
    if (!conferencia) return [];
    const corpos = new Map(conferencia.corpos.map((c) => [c.nome, c]));
    return conferencia.sobreposicoes
      .filter((s) => [s.corpoA, s.corpoB].every((nome) => ['METAL', 'SEM_MATERIAL'].includes(corpos.get(nome)?.uso ?? '')))
      .map((s) => ({ sobreposicao: s, corpos: maiorPrimeiro(s, corpos), impedimento: impedimento(s, corpos) }));
  });

  constructor() {
    effect(() => {
      const dialogo = this.dialogo().nativeElement;
      if (this.aberta() && !dialogo.open) dialogo.showModal();
      else if (!this.aberta() && dialogo.open) dialogo.close();
    });
  }

  /** Material do corpo como está no arquivo, ou o escolhido e ainda não aplicado; vazio: sem material do ateliê. */
  protected materialDe(corpo: CorpoConferidoDto, conferencia: ConferenciaDoModeloDto): string {
    const escolhido = this.escolhas()[corpo.nome];
    if (escolhido) return escolhido;
    return conferencia.materiais.some((m) => m.nome === corpo.material) ? (corpo.material ?? '') : '';
  }

  /** O material como aparece na tela, sem editar. */
  protected rotuloDoMaterial(corpo: CorpoConferidoDto, conferencia: ConferenciaDoModeloDto): string {
    if (corpo.uso === 'SEM_MATERIAL') return '— sem material —';
    return corpo.liga ?? conferencia.materiais.find((m) => m.nome === corpo.material)?.rotulo ?? corpo.material ?? '';
  }

  /** "5,00 g", "fora do peso", "aberto: fora do peso" ou vazio (sem material: o destaque já diz). */
  protected pesoDe(corpo: CorpoConferidoDto): string {
    if (corpo.contaNoPeso && corpo.massaGramas !== null) return gramas(corpo.massaGramas);
    if (corpo.uso === 'SEM_MATERIAL') return '';
    return corpo.fechado ? 'fora do peso' : 'aberto: fora do peso';
  }

  protected escolher(corpo: CorpoConferidoDto, material: string, conferencia: ConferenciaDoModeloDto): void {
    const atual = conferencia.materiais.some((m) => m.nome === corpo.material) ? corpo.material : null;
    this.escolhas.update((escolhas) => {
      const outras = Object.fromEntries(Object.entries(escolhas).filter(([nome]) => nome !== corpo.nome));
      return material && material !== atual ? { ...outras, [corpo.nome]: material } : outras;
    });
  }

  protected async aplicar(conferencia: ConferenciaDoModeloDto): Promise<void> {
    const corpos = Object.entries(this.escolhas()).map(([corpo, material]) => ({ corpo, material }));
    if (corpos.length === 0 || conferencia.arquivoId === null) return;
    const descricao = corpos.length === 1 ? 'Material aplicado.' : `Material aplicado em ${corpos.length} corpos.`;
    await this.alterar(this.modelo3d.putMateriais(this.projetoId(), this.versaoId(), { arquivoId: conferencia.arquivoId, corpos }), descricao);
  }

  protected async unir(item: SobreposicaoNaTela, conferencia: ConferenciaDoModeloDto): Promise<void> {
    if (conferencia.arquivoId === null) return;
    await this.alterar(
      this.modelo3d.postUnirCorpos(this.projetoId(), this.versaoId(), { arquivoId: conferencia.arquivoId, corpos: item.corpos }),
      `${item.corpos[0]} e ${item.corpos[1]} viraram um corpo só.`,
    );
  }

  /** Volta ao arquivo de antes da última alteração (uma cópia dele vira a atual: o histórico só cresce). */
  protected async desfazer(): Promise<void> {
    const ultima = this.ultima();
    if (!ultima || this.salvando()) return;
    this.salvando.set(true);
    try {
      const arquivoId = await firstValueFrom(this.ferramentas.postRestaurar(this.projetoId(), this.versaoId(), FERRAMENTA, ultima.anteriorId));
      this.ultima.set(null);
      this.recarregar();
      if (arquivoId !== null) this.alterado.emit(arquivoId);
      this.toast.sucesso('Alteração desfeita.');
    } catch (erro) {
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível desfazer.');
    } finally {
      this.salvando.set(false);
    }
  }

  protected fechar(): void {
    this.escolhas.set({});
    this.confirmandoUnir.set(null);
    this.ultima.set(null);
    this.fechado.emit();
  }

  private async alterar(requisicao: Observable<ModeloAlteradoDto>, descricao: string): Promise<void> {
    if (this.salvando() || this.ocupado()) return;
    this.salvando.set(true);
    try {
      const alterado = await firstValueFrom(requisicao);
      this.confirmandoUnir.set(null);
      this.ultima.set({ descricao, anteriorId: alterado.arquivoAnteriorId });
      this.recarregar();
      this.alterado.emit(alterado.arquivoId);
    } catch (erro) {
      // O erro HTTP já aparece pelo interceptor (ex.: o modelo mudou: a conferência relê).
      if (!(erro instanceof HttpErrorResponse)) this.toast.erro('Não foi possível alterar o modelo.');
      else this.recarregar();
    } finally {
      this.salvando.set(false);
    }
  }

  private recarregar(): void {
    this.escolhas.set({});
    this.recarga.update((n) => n + 1);
  }

}

function maiorPrimeiro(s: SobreposicaoConferidaDto, corpos: Map<string, CorpoConferidoDto>): readonly [string, string] {
  const volume = (nome: string) => corpos.get(nome)?.volumeMm3 ?? 0;
  return volume(s.corpoB) > volume(s.corpoA) ? [s.corpoB, s.corpoA] : [s.corpoA, s.corpoB];
}

function impedimento(s: SobreposicaoConferidaDto, corpos: Map<string, CorpoConferidoDto>): string | null {
  if (s.podeUnir) return null;
  const semMaterial = [s.corpoA, s.corpoB].some((nome) => corpos.get(nome)?.uso === 'SEM_MATERIAL');
  return semMaterial ? 'Defina o material para unir.' : 'Ligas diferentes: ajuste a forma no Modelo 3D.';
}

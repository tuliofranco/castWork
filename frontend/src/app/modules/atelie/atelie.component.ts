import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AtelieService } from '@data/atelie/atelie.service';
import type { ConfiguracaoAtelieDto } from '@data/atelie/dto/atelie.dto';
import { aplicarErrosDoServidor } from '@shared/forms/aplicar-erros-do-servidor';
import { ToastService } from '@shared/services/toast.service';
import { BarraAcoesComponent } from '@shared/ui/barra-acoes.component';
import { CabecalhoPaginaComponent } from '@shared/ui/cabecalho-pagina.component';
import { CampoComponent } from '@shared/ui/campo.component';
import { EstadoListaComponent } from '@shared/ui/estado-lista.component';
import { finalize } from 'rxjs';

const COR_HEX = /^#[0-9A-Fa-f]{6}$/;
const LOGO_MAXIMO = 1024 * 1024;

/**
 * Dados e marca do ateliê: o que aparece no cabeçalho e no rodapé dos documentos enviados ao cliente.
 */
@Component({
  selector: 'cw-atelie',
  imports: [ReactiveFormsModule, CabecalhoPaginaComponent, CampoComponent, BarraAcoesComponent, EstadoListaComponent],
  templateUrl: './atelie.component.html',
  styleUrl: './atelie.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AtelieComponent {
  private readonly servico = inject(AtelieService);
  private readonly toast = inject(ToastService);

  protected readonly carregando = signal(true);
  protected readonly erro = signal(false);
  protected readonly salvando = signal(false);
  protected readonly enviandoLogo = signal(false);
  protected readonly errosGerais = signal<readonly string[]>([]);
  protected readonly logoUrl = signal<string | null>(null);

  /** Campos que esta tela não edita, devolvidos como estão ao salvar. */
  /** Os valores como estão salvos: o Desfazer volta a eles aqui mesmo, sem reler do servidor. */
  private salvos: ReturnType<typeof this.form.getRawValue> | null = null;
  private preservado: Pick<ConfiguracaoAtelieDto, 'endereco'> | null = null;

  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    assinatura: ['', Validators.maxLength(150)],
    corMarca: ['#2E6E68', [Validators.required, Validators.pattern(COR_HEX)]],
    telefone: ['', Validators.maxLength(20)],
    email: ['', [Validators.email, Validators.maxLength(150)]],
    instagram: ['', Validators.maxLength(60)],
    site: ['', Validators.maxLength(150)],
    documento: [''],
    avisoBriefing: ['', Validators.maxLength(500)],
  });

  constructor() {
    this.carregar();
    inject(DestroyRef).onDestroy(() => this.trocarLogoUrl(null));
  }

  protected desfazer(): void {
    if (!this.salvos) return;
    this.form.reset(this.salvos);
    this.errosGerais.set([]);
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erro.set(false);
    this.servico
      .getConfiguracao()
      .pipe(finalize(() => this.carregando.set(false)))
      .subscribe({
        next: (c) => {
          this.preservado = { endereco: c.endereco };
          this.salvos = {
            nome: c.nome,
            assinatura: c.assinatura ?? '',
            corMarca: c.corMarca,
            telefone: c.telefone ?? '',
            email: c.email ?? '',
            instagram: c.instagram ?? '',
            site: c.site ?? '',
            documento: c.documento ?? '',
            avisoBriefing: c.avisoBriefing ?? '',
          };
          this.form.setValue(this.salvos);
          if (c.possuiLogo) this.carregarLogo();
          else this.trocarLogoUrl(null);
        },
        error: () => this.erro.set(true),
      });
  }

  /** O seletor de cor nativo e o campo de texto editam o mesmo valor. */
  protected escolherCor(evento: Event): void {
    this.form.controls.corMarca.setValue((evento.target as HTMLInputElement).value.toUpperCase());
    this.form.controls.corMarca.markAsDirty();
  }

  protected enviarLogo(evento: Event): void {
    const entrada = evento.target as HTMLInputElement;
    const arquivo = entrada.files?.[0];
    entrada.value = '';
    if (!arquivo) return;
    if (!['image/png', 'image/jpeg'].includes(arquivo.type)) {
      this.toast.erro('Envie o logo em PNG ou JPEG.');
      return;
    }
    if (arquivo.size > LOGO_MAXIMO) {
      this.toast.erro('O logo deve ter até 1 MB.');
      return;
    }
    this.enviandoLogo.set(true);
    this.servico
      .putLogo(arquivo)
      .pipe(finalize(() => this.enviandoLogo.set(false)))
      .subscribe(() => {
        this.toast.sucesso('Logo atualizado.');
        this.carregarLogo();
      });
  }

  protected removerLogo(): void {
    this.enviandoLogo.set(true);
    this.servico
      .deleteLogo()
      .pipe(finalize(() => this.enviandoLogo.set(false)))
      .subscribe(() => {
        this.toast.sucesso('Logo removido. Os documentos mostram só o nome.');
        this.trocarLogoUrl(null);
      });
  }

  protected salvar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.salvando.set(true);
    this.errosGerais.set([]);
    this.servico
      .putConfiguracao({
        nome: v.nome,
        assinatura: v.assinatura || null,
        corMarca: v.corMarca,
        telefone: v.telefone || null,
        email: v.email || null,
        instagram: v.instagram || null,
        site: v.site || null,
        documento: v.documento || null,
        avisoBriefing: v.avisoBriefing || null,
        endereco: this.preservado?.endereco ?? null,
      })
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: () => {
          this.form.markAsPristine();
          this.salvos = this.form.getRawValue();
          this.toast.sucesso('Marca salva. Os próximos documentos já saem com ela.');
        },
        error: (erro: unknown) => this.errosGerais.set(aplicarErrosDoServidor(this.form, erro)),
      });
  }

  private carregarLogo(): void {
    this.servico.getLogo().subscribe((blob) => this.trocarLogoUrl(blob.size > 0 ? URL.createObjectURL(blob) : null));
  }

  private trocarLogoUrl(url: string | null): void {
    const anterior = this.logoUrl();
    if (anterior) URL.revokeObjectURL(anterior);
    this.logoUrl.set(url);
  }
}

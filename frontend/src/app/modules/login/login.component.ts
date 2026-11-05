import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { finalize } from 'rxjs';

import { retornoSeguro } from './retorno-seguro';

@Component({
  selector: 'cw-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Chega do query param `?returnTo=` via withComponentInputBinding. */
  readonly returnTo = input<string>();

  // nonNullable: `reset()` volta para '' em vez de null, e o tipo é string (não string | null).
  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', Validators.required],
  });

  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected entrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);

    this.auth
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl(retornoSeguro(this.returnTo())),
        error: (erro: unknown) => this.erro.set(this.mensagemDeErro(erro)),
      });
  }

  private mensagemDeErro(erro: unknown): string {
    if (erro instanceof HttpErrorResponse && erro.status === 401) return 'E-mail ou senha inválidos.';
    return 'Não foi possível entrar. Tente novamente.';
  }
}

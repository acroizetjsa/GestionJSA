import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div style="min-height:100vh;display:grid;place-items:center;padding:1rem;">
      <div class="card" style="width:min(420px,100%);">
        <h1>GMAO Bus HVAC</h1>
        <p class="muted">Connexion à l'application de maintenance climatisation / chauffage bus.</p>

        @if (error()) {
          <div class="error" style="margin-bottom:1rem;">{{ error() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="grid">
          <div>
            <label for="username">Utilisateur</label>
            <input id="username" type="text" formControlName="username" placeholder="admin" />
          </div>

          <div>
            <label for="password">Mot de passe</label>
            <input id="password" type="password" formControlName="password" placeholder="••••••••" />
          </div>

          <button class="primary" [disabled]="form.invalid || loading()">{{ loading() ? 'Connexion...' : 'Se connecter' }}</button>
        </form>

        <div style="margin-top:1rem;" class="muted">
          Comptes seed: <strong>admin</strong> / <strong>tech1</strong> avec mot de passe <strong>ChangeMe123!</strong>
        </div>
      </div>
    </div>
  `
})
export class LoginPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = new FormGroup({
    username: new FormControl('admin', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('ChangeMe123!', { nonNullable: true, validators: [Validators.required] })
  });

  submit() {
    if (this.form.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: error => {
        this.error.set(error?.error?.message ?? 'Connexion impossible.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });
  }
}

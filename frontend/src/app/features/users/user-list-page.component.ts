import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

interface UserSummary {
  userId: number;
  username: string;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  roleCode: string;
  isActive: boolean;
  createdAtUtc: string;
  lastLoginAtUtc?: string | null;
}

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h1 style="margin:0;">Utilisateurs</h1>
          <p class="muted">Administration des comptes internes.</p>
        </div>
      </div>

      @if (error()) {
        <div class="error" style="margin-bottom:1rem;">{{ error() }}</div>
      }

      <div class="card">
        <table>
          <thead>
            <tr>
              <th>Nom</th>
              <th>Utilisateur</th>
              <th>Rôle</th>
              <th>Email</th>
              <th>Dernière connexion</th>
            </tr>
          </thead>
          <tbody>
            @for (user of users(); track user.userId) {
              <tr>
                <td>{{ user.fullName }}</td>
                <td>{{ user.username }}</td>
                <td>{{ user.roleCode }}</td>
                <td>{{ user.email || '-' }}</td>
                <td>{{ user.lastLoginAtUtc || '-' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class UserListPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly users = signal<UserSummary[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.get<UserSummary[]>('/users').subscribe({
      next: users => this.users.set(users),
      error: () => this.error.set('Impossible de charger les utilisateurs.')
    });
  }
}

import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div style="display:grid;grid-template-columns:280px 1fr;min-height:100vh;">
      <aside style="border-right:1px solid var(--border);background:var(--surface);padding:1rem;">
        <h2 style="margin-top:0;">GMAO Bus HVAC</h2>
        <p class="muted">{{ auth.session()?.fullName }}<br /><strong>{{ auth.role() }}</strong></p>

        <nav style="display:grid;gap:0.5rem;margin-top:1rem;">
          <a routerLink="/" routerLinkActive="active" class="nav-link">Dashboard</a>
          <a routerLink="/clients" routerLinkActive="active" class="nav-link">Clients / Sites</a>
          <a routerLink="/buses" routerLinkActive="active" class="nav-link">Flotte bus</a>
          <a routerLink="/work-orders" routerLinkActive="active" class="nav-link">Ordres de travail</a>
          <a routerLink="/time-tracking" routerLinkActive="active" class="nav-link">Pointages</a>
          <a routerLink="/parts" routerLinkActive="active" class="nav-link">Pièces / Stock</a>
          @if (auth.role() === 'ADMIN') {
            <a routerLink="/purchase-orders" routerLinkActive="active" class="nav-link">Achats</a>
            <a routerLink="/invoices" routerLinkActive="active" class="nav-link">Factures</a>
            <a routerLink="/users" routerLinkActive="active" class="nav-link">Utilisateurs</a>
          }
        </nav>

        <button class="secondary" style="margin-top:2rem;width:100%;" (click)="auth.logout()">Se déconnecter</button>
      </aside>

      <main>
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    .nav-link {
      color: var(--text);
      text-decoration: none;
      padding: 0.8rem 1rem;
      border-radius: 10px;
      border: 1px solid transparent;
    }
    .nav-link.active,
    .nav-link:hover {
      background: var(--surface-2);
      border-color: var(--border);
    }
    @media (max-width: 960px) {
      :host > div {
        grid-template-columns: 1fr !important;
      }
    }
  `]
})
export class ShellComponent {
  readonly auth = inject(AuthService);
}

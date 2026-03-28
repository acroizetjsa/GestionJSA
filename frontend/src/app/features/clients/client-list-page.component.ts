import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

interface ClientSummary {
  clientId: number;
  code: string;
  name: string;
  defaultHourlyRateExclTax: number;
  paymentTermDays: number;
  isActive: boolean;
  activeSiteCount: number;
}

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h1 style="margin:0;">Clients / Sites</h1>
          <p class="muted">Référentiel des clients et ateliers d'intervention.</p>
        </div>
      </div>

      @if (error()) {
        <div class="error" style="margin-bottom:1rem;">{{ error() }}</div>
      }

      <div class="card">
        <table>
          <thead>
            <tr>
              <th>Code</th>
              <th>Client</th>
              <th>Taux horaire</th>
              <th>Délai paiement</th>
              <th>Sites actifs</th>
            </tr>
          </thead>
          <tbody>
            @for (client of clients(); track client.clientId) {
              <tr>
                <td>{{ client.code }}</td>
                <td>{{ client.name }}</td>
                <td>{{ client.defaultHourlyRateExclTax | number:'1.2-2' }} €</td>
                <td>{{ client.paymentTermDays }} j</td>
                <td>{{ client.activeSiteCount }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class ClientListPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly clients = signal<ClientSummary[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.get<ClientSummary[]>('/clients').subscribe({
      next: data => this.clients.set(data),
      error: () => this.error.set('Impossible de charger les clients.')
    });
  }
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

interface BusSummary {
  busId: number;
  fleetNumber: string;
  registrationNumber?: string | null;
  vin?: string | null;
  brand?: string | null;
  model?: string | null;
  yearOfManufacture?: number | null;
  currentMileageKm: number;
  statusCode: string;
  clientName: string;
  siteName?: string | null;
}

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h1 style="margin:0;">Flotte de bus</h1>
          <p class="muted">Suivi du parc et de son rattachement client / site.</p>
        </div>
      </div>

      @if (error()) {
        <div class="error" style="margin-bottom:1rem;">{{ error() }}</div>
      }

      <div class="card">
        <table>
          <thead>
            <tr>
              <th>Parc</th>
              <th>Immatriculation</th>
              <th>Client</th>
              <th>Site</th>
              <th>Modèle</th>
              <th>Kilométrage</th>
            </tr>
          </thead>
          <tbody>
            @for (bus of buses(); track bus.busId) {
              <tr>
                <td>{{ bus.fleetNumber }}</td>
                <td>{{ bus.registrationNumber || '-' }}</td>
                <td>{{ bus.clientName }}</td>
                <td>{{ bus.siteName || '-' }}</td>
                <td>{{ bus.brand || '' }} {{ bus.model || '' }}</td>
                <td>{{ bus.currentMileageKm }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class BusListPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly buses = signal<BusSummary[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.get<BusSummary[]>('/buses').subscribe({
      next: data => this.buses.set(data),
      error: () => this.error.set('Impossible de charger la flotte de bus.')
    });
  }
}

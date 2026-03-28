import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

interface PartSummary {
  partId: number;
  partNumber: string;
  name: string;
  unitCode: string;
  purchasePriceExclTax: number;
  salePriceExclTax: number;
  minimumStock: number;
  quantityOnHand: number;
}

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h1 style="margin:0;">Pièces / Stock</h1>
          <p class="muted">Vue globale sur les pièces et le stock disponible.</p>
        </div>
      </div>

      @if (error()) {
        <div class="error" style="margin-bottom:1rem;">{{ error() }}</div>
      }

      <div class="card">
        <table>
          <thead>
            <tr>
              <th>Référence</th>
              <th>Nom</th>
              <th>Stock</th>
              <th>Mini</th>
              <th>Achat HT</th>
              <th>Vente HT</th>
            </tr>
          </thead>
          <tbody>
            @for (part of parts(); track part.partId) {
              <tr>
                <td>{{ part.partNumber }}</td>
                <td>{{ part.name }}</td>
                <td>{{ part.quantityOnHand }}</td>
                <td>{{ part.minimumStock }}</td>
                <td>{{ part.purchasePriceExclTax | number:'1.2-2' }} €</td>
                <td>{{ part.salePriceExclTax | number:'1.2-2' }} €</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class PartListPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly parts = signal<PartSummary[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.get<PartSummary[]>('/parts').subscribe({
      next: data => this.parts.set(data),
      error: () => this.error.set('Impossible de charger les pièces.')
    });
  }
}

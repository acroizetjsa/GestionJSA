import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

interface PurchaseOrderSummary {
  purchaseOrderId: number;
  purchaseOrderNumber: string;
  supplierName: string;
  siteName?: string | null;
  statusCode: string;
  orderedAtUtc: string;
  expectedAtUtc?: string | null;
  totalExclTax: number;
  totalInclTax: number;
}

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h1 style="margin:0;">Commandes d'achat</h1>
          <p class="muted">Pilotage des achats et des réceptions de stock.</p>
        </div>
      </div>

      @if (error()) {
        <div class="error" style="margin-bottom:1rem;">{{ error() }}</div>
      }

      <div class="card">
        <table>
          <thead>
            <tr>
              <th>N° commande</th>
              <th>Fournisseur</th>
              <th>Site</th>
              <th>Statut</th>
              <th>Total HT</th>
              <th>Total TTC</th>
            </tr>
          </thead>
          <tbody>
            @for (po of purchaseOrders(); track po.purchaseOrderId) {
              <tr>
                <td>{{ po.purchaseOrderNumber }}</td>
                <td>{{ po.supplierName }}</td>
                <td>{{ po.siteName || '-' }}</td>
                <td>{{ po.statusCode }}</td>
                <td>{{ po.totalExclTax | number:'1.2-2' }} €</td>
                <td>{{ po.totalInclTax | number:'1.2-2' }} €</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class PurchaseOrderListPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly purchaseOrders = signal<PurchaseOrderSummary[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.get<PurchaseOrderSummary[]>('/purchase-orders').subscribe({
      next: data => this.purchaseOrders.set(data),
      error: () => this.error.set('Impossible de charger les commandes d\'achat.')
    });
  }
}

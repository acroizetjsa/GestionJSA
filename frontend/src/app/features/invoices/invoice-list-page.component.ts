import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

interface InvoiceSummary {
  invoiceId: number;
  invoiceNumber: string;
  clientName: string;
  siteName?: string | null;
  statusCode: string;
  issueDate: string;
  dueDate: string;
  totalInclTax: number;
  paidAmount: number;
  balance: number;
}

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h1 style="margin:0;">Factures et règlements</h1>
          <p class="muted">Suivi de l'émission et du pointage des paiements.</p>
        </div>
      </div>

      @if (error()) {
        <div class="error" style="margin-bottom:1rem;">{{ error() }}</div>
      }

      <div class="card">
        <table>
          <thead>
            <tr>
              <th>N° facture</th>
              <th>Client</th>
              <th>Statut</th>
              <th>TTC</th>
              <th>Payé</th>
              <th>Reste dû</th>
            </tr>
          </thead>
          <tbody>
            @for (invoice of invoices(); track invoice.invoiceId) {
              <tr>
                <td>{{ invoice.invoiceNumber }}</td>
                <td>{{ invoice.clientName }}</td>
                <td>{{ invoice.statusCode }}</td>
                <td>{{ invoice.totalInclTax | number:'1.2-2' }} €</td>
                <td>{{ invoice.paidAmount | number:'1.2-2' }} €</td>
                <td>{{ invoice.balance | number:'1.2-2' }} €</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class InvoiceListPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly invoices = signal<InvoiceSummary[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.get<InvoiceSummary[]>('/invoices').subscribe({
      next: data => this.invoices.set(data),
      error: () => this.error.set('Impossible de charger les factures.')
    });
  }
}

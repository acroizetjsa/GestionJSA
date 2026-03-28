import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

interface WorkOrderSummary {
  workOrderId: number;
  workOrderNumber: string;
  typeCode: string;
  statusCode: string;
  priorityCode: string;
  clientName: string;
  siteName?: string | null;
  busLabel?: string | null;
  title: string;
  assignedTechnician?: string | null;
  reportedAtUtc: string;
  scheduledStartUtc?: string | null;
  closedAtUtc?: string | null;
}

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h1 style="margin:0;">Ordres de travail</h1>
          <p class="muted">Maintenance préventive et curative sur climatisations / chauffages.</p>
        </div>
      </div>

      @if (error()) {
        <div class="error" style="margin-bottom:1rem;">{{ error() }}</div>
      }

      <div class="card">
        <table>
          <thead>
            <tr>
              <th>N° OT</th>
              <th>Titre</th>
              <th>Type</th>
              <th>Priorité</th>
              <th>Statut</th>
              <th>Bus</th>
              <th>Technicien</th>
            </tr>
          </thead>
          <tbody>
            @for (workOrder of workOrders(); track workOrder.workOrderId) {
              <tr>
                <td>{{ workOrder.workOrderNumber }}</td>
                <td>{{ workOrder.title }}</td>
                <td>{{ workOrder.typeCode }}</td>
                <td>{{ workOrder.priorityCode }}</td>
                <td>{{ workOrder.statusCode }}</td>
                <td>{{ workOrder.busLabel || '-' }}</td>
                <td>{{ workOrder.assignedTechnician || '-' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class WorkOrderListPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly workOrders = signal<WorkOrderSummary[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.get<WorkOrderSummary[]>('/work-orders').subscribe({
      next: data => this.workOrders.set(data),
      error: () => this.error.set('Impossible de charger les ordres de travail.')
    });
  }
}

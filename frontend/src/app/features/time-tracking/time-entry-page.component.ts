import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { GeolocationService } from '../../core/services/geolocation.service';

interface TimeEntry {
  timeEntryId: number;
  technicianId: number;
  workOrderId?: number | null;
  startAtUtc: string;
  endAtUtc?: string | null;
  statusCode: string;
  notes?: string | null;
}

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h1 style="margin:0;">Pointages horaires</h1>
          <p class="muted">Capture de début et fin avec géolocalisation navigateur.</p>
        </div>
      </div>

      @if (error()) {
        <div class="error" style="margin-bottom:1rem;">{{ error() }}</div>
      }

      <div class="grid grid-2">
        <div class="card">
          <h3>Pointage en cours</h3>
          @if (openEntry()) {
            <p>Un pointage est ouvert depuis <strong>{{ openEntry()?.startAtUtc }}</strong>.</p>
            <button class="danger" [disabled]="busy()" (click)="stop()">Arrêter le pointage</button>
          } @else {
            <form [formGroup]="form" class="grid">
              <div>
                <label for="workOrderId">OT lié (optionnel)</label>
                <input id="workOrderId" type="number" formControlName="workOrderId" placeholder="Identifiant OT" />
              </div>
              <div>
                <label for="notes">Commentaire</label>
                <textarea id="notes" rows="3" formControlName="notes" placeholder="Départ atelier, intervention sur site, etc."></textarea>
              </div>
              <button type="button" class="primary" [disabled]="busy()" (click)="start()">Démarrer le pointage</button>
            </form>
          }
        </div>

        <div class="card">
          <h3>Rappel usage</h3>
          <ul>
            <li>Autoriser la géolocalisation dans le navigateur.</li>
            <li>Utiliser l'application en HTTPS.</li>
            <li>Capturer le début et la fin de présence terrain.</li>
          </ul>
        </div>
      </div>

      <div class="card" style="margin-top:1rem;">
        <h3>Historique récent</h3>
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>OT</th>
              <th>Début</th>
              <th>Fin</th>
              <th>Statut</th>
              <th>Commentaire</th>
            </tr>
          </thead>
          <tbody>
            @for (entry of entries(); track entry.timeEntryId) {
              <tr>
                <td>{{ entry.timeEntryId }}</td>
                <td>{{ entry.workOrderId || '-' }}</td>
                <td>{{ entry.startAtUtc }}</td>
                <td>{{ entry.endAtUtc || '-' }}</td>
                <td>{{ entry.statusCode }}</td>
                <td>{{ entry.notes || '-' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class TimeEntryPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly geolocation = inject(GeolocationService);

  readonly entries = signal<TimeEntry[]>([]);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly openEntry = computed(() => this.entries().find(entry => entry.statusCode === 'OPEN') ?? null);

  readonly form = new FormGroup({
    workOrderId: new FormControl<number | null>(null),
    notes: new FormControl('')
  });

  ngOnInit(): void {
    void this.load();
  }

  async start() {
    this.error.set(null);
    this.busy.set(true);
    try {
      const geo = await this.geolocation.getCurrentPosition();
      await firstValueFrom(this.api.post('/time-entries/start', {
        workOrderId: this.form.value.workOrderId,
        startLatitude: geo.latitude,
        startLongitude: geo.longitude,
        startAccuracyMeters: geo.accuracyMeters,
        notes: this.form.value.notes
      }));
      this.form.reset({ workOrderId: null, notes: '' });
      await this.load();
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : 'Impossible de démarrer le pointage.');
    } finally {
      this.busy.set(false);
    }
  }

  async stop() {
    const current = this.openEntry();
    if (!current) {
      return;
    }

    this.error.set(null);
    this.busy.set(true);
    try {
      const geo = await this.geolocation.getCurrentPosition();
      await firstValueFrom(this.api.post(`/time-entries/${current.timeEntryId}/stop`, {
        endLatitude: geo.latitude,
        endLongitude: geo.longitude,
        endAccuracyMeters: geo.accuracyMeters
      }));
      await this.load();
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : 'Impossible de stopper le pointage.');
    } finally {
      this.busy.set(false);
    }
  }

  private async load() {
    const data = await firstValueFrom(this.api.get<TimeEntry[]>('/time-entries'));
    this.entries.set(data);
  }
}

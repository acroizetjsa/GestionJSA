import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h1 style="margin:0;">Tableau de bord</h1>
          <p class="muted">Vue d'ensemble du périmètre GMAO bus CVC.</p>
        </div>
      </div>

      <div class="grid grid-3">
        <div class="card">
          <h3>Rôle connecté</h3>
          <p class="badge">{{ auth.role() }}</p>
          <p class="muted">Les menus et actions sont filtrés selon le profil.</p>
        </div>

        <div class="card">
          <h3>Flux métier</h3>
          <p>OT curatifs / préventifs, pointages terrain, stock, achats, factures et règlements.</p>
        </div>

        <div class="card">
          <h3>Usage terrain</h3>
          <p>Le module pointage permet de capturer la position au démarrage et à l'arrêt d'une intervention.</p>
        </div>
      </div>

      <div class="grid grid-2" style="margin-top:1rem;">
        <div class="card">
          <h3>Parcours recommandé</h3>
          <ol>
            <li>Créer le référentiel client / site / bus.</li>
            <li>Créer ou affecter les OT.</li>
            <li>Pointer les heures avec géolocalisation.</li>
            <li>Consommer les pièces.</li>
            <li>Facturer l'intervention.</li>
          </ol>
        </div>

        <div class="card">
          <h3>Compte de démo</h3>
          <p>Le starter prévoit deux comptes de démonstration pour tester les droits et les écrans.</p>
        </div>
      </div>
    </div>
  `
})
export class DashboardPageComponent {
  readonly auth = inject(AuthService);
}

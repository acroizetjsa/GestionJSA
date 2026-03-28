# Architecture fonctionnelle et technique

## 1. Périmètre métier

L'application couvre le cycle complet d'une activité de maintenance bus orientée CVC :

1. Référentiel clients, sites, bus et équipements.
2. Création et suivi des ordres de travail curatifs et préventifs.
3. Saisie des temps techniciens avec géolocalisation de début et fin.
4. Consommation de pièces et mise à jour du stock.
5. Génération de facture depuis une intervention.
6. Emission de la facture et pointage des règlements.
7. Réapprovisionnement via commandes d'achat et réception en stock.

## 2. Architecture cible

### Frontend

- Angular en mode SPA
- JWT stocké côté navigateur
- Route guards par rôle
- Services dédiés : auth, API, geolocation
- Ecrans principaux : dashboard, utilisateurs, clients, bus, OT, pointages, pièces, achats, factures

### API

- ASP.NET Core
- Contrôleurs REST par module métier
- ADO.NET natif via `Microsoft.Data.SqlClient`
- Authentification JWT
- Autorisation par rôle et par portée métier
- Transactions SQL sur les opérations critiques (facturation, consommation de stock, réception de commande)

### Base de données

- SQL Server
- Schémas séparés par domaine : `sec`, `crm`, `asset`, `ops`, `stock`, `buy`, `bill`
- Vues de calcul pour les soldes de facture et le stock courant
- Séquences SQL pour la numérotation des OT, factures et commandes d'achat

## 3. Principes de conception

- Aucun Entity Framework.
- Toutes les requêtes passent par des repositories ADO.NET explicites.
- Toutes les requêtes d'écriture sont paramétrées.
- Les rôles sont simples au départ : `ADMIN` et `TECHNICIAN`.
- Les techniciens ne voient que les données utiles à leur activité terrain.
- Les référentiels sensibles (facturation, utilisateurs, achats) sont réservés à l'administrateur.

## 4. Flux métier majeurs

### 4.1 Maintenance curative

1. L'administrateur crée un OT curatif.
2. Il affecte un technicien.
3. Le technicien pointe son départ / son arrivée.
4. Il consomme des pièces si nécessaire.
5. Il clôture l'OT avec temps passé et compte-rendu.
6. L'administrateur génère la facture.
7. Il émet la facture puis pointe le paiement.

### 4.2 Maintenance préventive

1. Un équipement bus possède un plan préventif.
2. L'équipe métier crée les OT préventifs selon un calendrier (lot manuel au départ, automatisable ensuite).
3. Le technicien exécute le contrôle et clôture l'OT.
4. La prochaine échéance est recalculée hors de ce starter, dans un batch ou service planifié à ajouter.

## 5. Sécurité

- JWT avec claims utilisateur et rôle.
- API sécurisée par attributs `[Authorize]`.
- Filtrage applicatif complémentaire pour empêcher un technicien de manipuler des OT non affectés.
- Journalisation minimale à compléter dans une itération ultérieure (audit métier et logs techniques).

## 6. Non fonctionnel

- Déploiement recommandé : Angular statique + API ASP.NET Core derrière IIS ou reverse proxy + SQL Server.
- HTTPS obligatoire pour exploiter correctement la géolocalisation navigateur.
- Possibilité d'évolution vers une Progressive Web App pour un meilleur usage mobile terrain.

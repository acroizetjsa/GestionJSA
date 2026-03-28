# GMAO Bus HVAC - Starter Kit

Ce dépôt pose une base exploitable pour une GMAO orientée maintenance préventive et curative sur climatisations et chauffages de bus.

## Contenu

- `database/` : scripts SQL Server de création et de pré-chargement.
- `backend/` : API ASP.NET Core structurée en modules métier, avec accès aux données en ADO.NET via `Microsoft.Data.SqlClient`.
- `frontend/` : socle Angular avec authentification JWT, gestion des rôles, écran de pointage horaire géolocalisé et écrans de consultation principaux.
- `docs/` : architecture, backlog, matrice d'accès et catalogue d'API.

## Modules couverts

- Authentification et gestion des utilisateurs (`ADMIN`, `TECHNICIAN`)
- Clients et sites d'intervention
- Flotte de bus
- Ordres de travail préventifs et curatifs
- Pointage horaire avec géolocalisation de début/fin
- Facturation, émission et pointage des paiements
- Pièces détachées, stock et mouvements de stock
- Commandes d'achat et réception en stock

## Hypothèses de cadrage

- Une seule société de maintenance exploite l'application.
- Les clients n'ont pas d'accès direct dans cette première version.
- La devise par défaut est l'euro.
- La géolocalisation n'est capturée qu'aux événements métier (début/fin d'intervention ou pointage), pas en suivi continu.
- Le socle est pensé pour une API REST consommée par Angular, avec JWT et contrôle d'accès par rôle.

## Démarrage rapide

### 1. Base de données

Exécuter dans l'ordre :

1. `database/001_init.sql`
2. `database/002_seed.sql`

### 2. API

Configurer :

- la chaîne de connexion SQL Server dans `backend/appsettings.json`
- la clé JWT dans `backend/appsettings.json`

Puis lancer :

```bash
dotnet restore
cd backend
dotnet run
```

### 3. Frontend

```bash
cd frontend
npm install
npm start
```

## Comptes de démonstration

Les scripts de seed créent deux comptes :

- `admin` / `ChangeMe123!`
- `tech1` / `ChangeMe123!`

Changer immédiatement les mots de passe après le premier démarrage.

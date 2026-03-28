# Matrice d'accès

| Module | ADMIN | TECHNICIAN |
|---|---|---|
| Authentification | Oui | Oui |
| Utilisateurs | CRUD | Non |
| Clients / Sites | CRUD | Lecture seule |
| Bus | CRUD | Lecture seule |
| Pièces / Stock | CRUD | Lecture seule + consommation sur OT affecté |
| Commandes d'achat | CRUD | Non |
| Ordres de travail | CRUD | Lecture de ses OT + clôture + consommation pièces |
| Pointages horaires | Tout | Ses pointages uniquement |
| Factures | CRUD | Non |
| Paiements | CRUD | Non |

## Règles métier d'accès

- Un technicien ne doit jamais voir les factures ni les paiements.
- Un technicien ne doit voir que ses OT affectés ou ses propres pointages.
- Un administrateur a accès à l'intégralité du périmètre.

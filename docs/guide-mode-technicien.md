# Guide utilisateur — Mode technicien

Ce guide décrit l’utilisation de la GMAO avec un compte disposant du profil `TECHNICIAN`.

Le mode technicien est volontairement limité aux fonctions nécessaires aux interventions terrain. Les fonctions d’administration, de facturation et d’achats restent réservées au profil administrateur.

## 1. Se connecter

1. Ouvrir l’application depuis un navigateur récent.
2. Saisir l’identifiant et le mot de passe communiqués par l’administrateur.
3. Valider la connexion.

Lors de la première connexion, remplacer le mot de passe temporaire si cette fonction est proposée.

Ne jamais partager ses identifiants. Les pointages et les actions effectuées dans l’application sont rattachés au compte connecté.

## 2. Périmètre du profil technicien

Un technicien peut :

- consulter les clients et leurs sites d’intervention ;
- consulter les bus enregistrés dans la flotte ;
- consulter uniquement les ordres de travail qui lui sont affectés ;
- consulter les pièces et le stock disponible ;
- déclarer les pièces consommées sur un ordre de travail qui lui est affecté ;
- démarrer et arrêter ses propres pointages horaires ;
- clôturer un ordre de travail qui lui est affecté lorsque l’intervention est terminée.

Un technicien ne peut pas :

- créer, modifier ou supprimer des utilisateurs ;
- modifier les clients, les sites ou les bus ;
- gérer les commandes d’achat ;
- consulter ou modifier les factures et les paiements ;
- consulter les ordres de travail affectés à d’autres techniciens ;
- modifier les pointages d’un autre utilisateur.

## 3. Consulter ses ordres de travail

Ouvrir le menu **Ordres de travail**.

La liste doit afficher uniquement les interventions affectées au technicien connecté.

Avant de partir sur site, vérifier au minimum :

- le numéro de l’ordre de travail ;
- le client ;
- le site d’intervention ;
- le bus concerné ;
- le type d’intervention : préventive ou curative ;
- la description du problème ou des travaux demandés ;
- la date prévue ;
- les éventuelles consignes de sécurité ou d’accès au site.

En cas d’erreur d’affectation, ne pas intervenir sur un ordre de travail non attribué. Contacter l’administrateur afin qu’il corrige l’affectation.

## 4. Démarrer un pointage géolocalisé

La géolocalisation est enregistrée uniquement lors des événements de début et de fin de pointage. L’application ne réalise pas de suivi permanent.

### Prérequis

- utiliser un smartphone, une tablette ou un ordinateur disposant d’un service de localisation ;
- autoriser le navigateur à accéder à la position ;
- utiliser l’application via une adresse sécurisée en `https` ;
- activer la localisation du téléphone avant le pointage.

### Procédure

1. Ouvrir l’ordre de travail concerné.
2. Vérifier qu’il s’agit du bon client, du bon site et du bon bus.
3. Appuyer sur **Démarrer le pointage**.
4. Autoriser l’accès à la position lorsque le navigateur le demande.
5. Attendre la confirmation de l’enregistrement.

Le pointage doit être lancé au début réel de l’intervention, sur le site concerné.

Ne pas lancer plusieurs pointages simultanément.

## 5. Pendant l’intervention

Pendant l’intervention :

- respecter les règles de sécurité du site ;
- identifier précisément le bus avant toute opération ;
- noter les symptômes constatés ;
- consigner le diagnostic réalisé ;
- indiquer les travaux effectués ;
- enregistrer les pièces réellement utilisées ;
- signaler toute anomalie non prévue ou toute intervention complémentaire nécessaire.

Lorsqu’une pièce est consommée, vérifier la référence et la quantité avant validation. Une consommation de stock incorrecte fausse l’état du stock et le coût de l’intervention.

## 6. Terminer le pointage

À la fin réelle de l’intervention :

1. vérifier que les informations de l’intervention sont complètes ;
2. vérifier les pièces consommées ;
3. saisir les observations utiles ;
4. appuyer sur **Arrêter le pointage** ;
5. autoriser à nouveau l’accès à la position si le navigateur le demande ;
6. attendre la confirmation de l’enregistrement de l’heure et de la géolocalisation de fin.

Ne pas arrêter le pointage avant la fin effective de l’intervention.

Si l’intervention est interrompue puis reprise plus tard, suivre la règle définie par JSA-Services concernant les pauses et les déplacements. En cas de doute, demander une consigne à l’administrateur avant de modifier le pointage.

## 7. Clôturer un ordre de travail

Un ordre de travail ne doit être clôturé que lorsque :

- l’intervention est réellement terminée ;
- le pointage est arrêté ;
- les travaux réalisés sont renseignés ;
- les pièces consommées sont enregistrées ;
- les observations sont complètes ;
- aucun complément immédiat n’est nécessaire.

Si l’intervention n’est pas terminée, si une pièce manque ou si un nouveau passage est nécessaire, ne pas clôturer l’ordre de travail. Ajouter une observation et prévenir l’administrateur.

## 8. Consulter les clients, sites et bus

Le technicien dispose d’un accès en lecture seule.

### Clients et sites

Les informations permettent notamment de vérifier :

- le nom du client ;
- l’adresse du site ;
- l’atelier concerné ;
- les informations d’accès ;
- les coordonnées utiles disponibles.

### Bus

Avant l’intervention, contrôler l’identité du véhicule à partir des informations enregistrées, par exemple :

- numéro de parc ;
- immatriculation ;
- marque et modèle ;
- client et site de rattachement.

Toute incohérence doit être signalée à l’administrateur. Le profil technicien ne modifie pas directement la fiche du véhicule.

## 9. Consulter les pièces et le stock

Le mode technicien permet de consulter les références de pièces et le stock disponible.

Avant de déclarer une consommation :

- vérifier la référence exacte ;
- vérifier la désignation ;
- vérifier la quantité ;
- vérifier que la pièce est consommée sur le bon ordre de travail.

Le technicien ne crée pas de commande d’achat. Une rupture ou un besoin de réapprovisionnement doit être signalé à l’administrateur.

## 10. Problèmes fréquents

### La géolocalisation est refusée

- ouvrir les paramètres du navigateur ;
- autoriser la localisation pour le site de l’application ;
- vérifier que la localisation du téléphone est activée ;
- recharger la page puis recommencer.

### La position ne peut pas être déterminée

- se placer dans une zone avec une meilleure réception GPS ;
- désactiver puis réactiver la localisation ;
- vérifier la connexion Internet ;
- attendre quelques secondes avant de relancer le pointage.

### L’ordre de travail n’apparaît pas

L’ordre de travail n’est probablement pas affecté au compte connecté. Contacter l’administrateur en indiquant le client, le site, le bus et la date prévue.

### Une pièce n’apparaît pas dans le stock

Ne pas sélectionner une référence approchante. Relever la référence exacte de la pièce et demander sa création ou sa correction à l’administrateur.

### Le pointage n’a pas été arrêté

Ne pas créer un nouveau pointage pour corriger seul l’erreur. Signaler immédiatement l’oubli à l’administrateur avec l’heure réelle de fin d’intervention.

### L’application affiche une erreur d’accès

Vérifier que l’action demandée appartient bien au périmètre technicien. Les fonctions de facturation, paiements, utilisateurs et commandes d’achat sont réservées aux administrateurs.

## 11. Bonnes pratiques terrain

- Se connecter avec son compte personnel.
- Vérifier l’ordre de travail avant de pointer.
- Autoriser la géolocalisation uniquement pour l’application de GMAO.
- Pointer au début et à la fin réels de l’intervention.
- Saisir des observations compréhensibles par une autre personne.
- Indiquer les pièces et quantités réellement consommées.
- Ne pas clôturer une intervention incomplète.
- Signaler immédiatement toute erreur de pointage, d’affectation ou de stock.
- Se déconnecter après utilisation sur un appareil partagé.

## 12. Données de géolocalisation

La position est enregistrée lors du début et de la fin du pointage afin d’associer l’action au lieu de l’intervention.

Le socle actuel ne prévoit pas de suivi continu des déplacements du technicien.

L’utilisateur doit être informé de la finalité de cette collecte et des règles internes applicables à la conservation et à l’utilisation des données.

## 13. Limites de la version actuelle

Ce dépôt constitue un socle applicatif. Selon l’avancement du développement de l’interface, certaines actions décrites peuvent nécessiter encore un écran ou un formulaire complémentaire.

Les règles d’accès prévues restent les suivantes :

- lecture des clients, sites, bus et stock ;
- accès aux seuls ordres de travail affectés ;
- accès aux seuls pointages du technicien connecté ;
- clôture et consommation de pièces uniquement sur ses ordres de travail ;
- aucun accès aux factures, paiements, utilisateurs et commandes d’achat.

En cas d’écart entre ce guide et le comportement constaté, signaler le problème afin de vérifier les droits du compte ou l’état d’avancement du module concerné.

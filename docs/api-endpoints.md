# Catalogue d'API

## Auth

- `POST /api/auth/login`

## Utilisateurs

- `GET /api/users`
- `POST /api/users`

## Clients / Sites

- `GET /api/clients`
- `POST /api/clients`
- `GET /api/clients/{clientId}/sites`
- `POST /api/clients/{clientId}/sites`

## Bus

- `GET /api/buses`
- `POST /api/buses`

## Ordres de travail

- `GET /api/work-orders`
- `POST /api/work-orders`
- `POST /api/work-orders/{workOrderId}/close`
- `POST /api/work-orders/{workOrderId}/consume-parts`

## Pointages horaires

- `GET /api/time-entries`
- `POST /api/time-entries/start`
- `POST /api/time-entries/{timeEntryId}/stop`

## Pièces / stock

- `GET /api/parts`
- `POST /api/parts`

## Commandes d'achat

- `GET /api/purchase-orders`
- `POST /api/purchase-orders`
- `POST /api/purchase-orders/{purchaseOrderId}/receive`

## Facturation

- `GET /api/invoices`
- `POST /api/invoices/from-work-order`
- `POST /api/invoices/{invoiceId}/issue`
- `POST /api/invoices/payments`

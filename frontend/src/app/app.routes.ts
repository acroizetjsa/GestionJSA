import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login-page.component').then(m => m.LoginPageComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/layout/shell.component').then(m => m.ShellComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./features/dashboard/dashboard-page.component').then(m => m.DashboardPageComponent)
      },
      {
        path: 'users',
        canActivate: [authGuard],
        data: { roles: ['ADMIN'] },
        loadComponent: () => import('./features/users/user-list-page.component').then(m => m.UserListPageComponent)
      },
      {
        path: 'clients',
        canActivate: [authGuard],
        data: { roles: ['ADMIN', 'TECHNICIAN'] },
        loadComponent: () => import('./features/clients/client-list-page.component').then(m => m.ClientListPageComponent)
      },
      {
        path: 'buses',
        canActivate: [authGuard],
        data: { roles: ['ADMIN', 'TECHNICIAN'] },
        loadComponent: () => import('./features/buses/bus-list-page.component').then(m => m.BusListPageComponent)
      },
      {
        path: 'work-orders',
        canActivate: [authGuard],
        data: { roles: ['ADMIN', 'TECHNICIAN'] },
        loadComponent: () => import('./features/work-orders/work-order-list-page.component').then(m => m.WorkOrderListPageComponent)
      },
      {
        path: 'time-tracking',
        canActivate: [authGuard],
        data: { roles: ['ADMIN', 'TECHNICIAN'] },
        loadComponent: () => import('./features/time-tracking/time-entry-page.component').then(m => m.TimeEntryPageComponent)
      },
      {
        path: 'parts',
        canActivate: [authGuard],
        data: { roles: ['ADMIN', 'TECHNICIAN'] },
        loadComponent: () => import('./features/parts/part-list-page.component').then(m => m.PartListPageComponent)
      },
      {
        path: 'purchase-orders',
        canActivate: [authGuard],
        data: { roles: ['ADMIN'] },
        loadComponent: () => import('./features/purchasing/purchase-order-list-page.component').then(m => m.PurchaseOrderListPageComponent)
      },
      {
        path: 'invoices',
        canActivate: [authGuard],
        data: { roles: ['ADMIN'] },
        loadComponent: () => import('./features/invoices/invoice-list-page.component').then(m => m.InvoiceListPageComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];

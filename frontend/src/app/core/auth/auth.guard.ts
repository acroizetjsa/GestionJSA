import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.parseUrl('/login');
  }

  const requiredRoles = route.data?.['roles'] as string[] | undefined;
  if (requiredRoles && !requiredRoles.includes(auth.role() ?? '')) {
    return router.parseUrl('/');
  }

  return true;
};

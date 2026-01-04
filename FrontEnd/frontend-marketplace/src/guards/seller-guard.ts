import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';
import { map, take } from 'rxjs';

export const sellerGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.currentUser$.pipe(
    take(1),
    map(user => {
      if (!user) {
        return router.createUrlTree(['/login']);
      }

      if (user.role === 'Admin') {
        return true;
      }

      if (user.role === 'Seller') {
        if (user.isApproved) {
          return true;
        }
        return router.createUrlTree(['/pending-approval']);
      }

      return router.createUrlTree(['/']);
    })
  );
};

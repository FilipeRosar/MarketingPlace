// guards/seller.guard.ts
import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth/auth.service';

export const sellerGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const user = authService.currentUserValue;

  if (!user) {
    router.navigate(['/login']);
    return false;
  }

  if (user.role === 'Admin') return true;

  if (user.role === 'Seller' && user.isApproved === false) {
    router.navigate(['/pending-approval']);
    return false;
  }

  return true;
};

import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';

export const adminGuard: CanMatchFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const user = authService.currentUserValue;

  if (user && user.role === 'Admin') {
    return true;
  }

  alert('Acesso negado. Apenas administradores.');
  return router.parseUrl('/');
};
